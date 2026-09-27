using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class KanbanEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public KanbanEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> CreateBoardAsync(string name = "Line 3 Kanban") =>
        await (await _client.PostAsJsonAsync("/api/boards", new { Name = name, ProjectId = (Guid?)null }))
            .Content.ReadFromJsonAsync<Guid>();

    private async Task<Guid> CreateColumnAsync(Guid boardId, string name = "To Do", int order = 0) =>
        await (await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { Name = name, Order = order }))
            .Content.ReadFromJsonAsync<Guid>();

    [Fact]
    public async Task PostBoard_WithValidPayload_ReturnsCreated()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-owner-1@example.com"));

        var response = await _client.PostAsJsonAsync("/api/boards", new { Name = "Line 3 Kanban", ProjectId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostBoard_WithProjectInAnotherOrganization_ReturnsBadRequest()
    {
        var otherOrgToken = await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-other-owner@example.com");
        UseToken(otherOrgToken);
        var otherOrgProjectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Other Org Project",
            Code = "OOP",
            StartDate = new DateOnly(2026, 1, 1)
        });
        var otherOrgProjectId = await otherOrgProjectResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-owner-2@example.com"));
        var response = await _client.PostAsJsonAsync("/api/boards", new { Name = "Line 3 Kanban", ProjectId = otherOrgProjectId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBoards_OnlyReturnsTheCallersOwnOrganization()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-org-a@example.com"));
        await CreateBoardAsync("Org A Board");

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-org-b@example.com"));
        await CreateBoardAsync("Org B Board");

        var response = await _client.GetAsync("/api/boards");
        var body = await response.Content.ReadFromJsonAsync<List<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle(); // only orgB's board, since the client is now logged in as orgB's user
    }

    [Fact]
    public async Task GetBoardById_ForBoardInAnotherOrganization_ReturnsBadRequest()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-owner-3@example.com"));
        var boardId = await CreateBoardAsync();

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-intruder-1@example.com"));
        var response = await _client.GetAsync($"/api/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostColumn_ThenPostCard_ThenGetBoard_ReturnsColumnWithCard()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "board-owner-4@example.com"));
        var boardId = await CreateBoardAsync();
        var columnId = await CreateColumnAsync(boardId);

        var cardResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns/{columnId}/cards", new
        {
            Title = "Install PLC",
            GanttTaskId = (Guid?)null
        });
        cardResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var boardResponse = await _client.GetAsync($"/api/boards/{boardId}");
        var json = await boardResponse.Content.ReadAsStringAsync();

        boardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("Install PLC");
    }

    [Fact]
    public async Task PatchAssignee_WithUserInSameOrganization_ReturnsNoContent()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "board-owner-5@example.com"));
        var boardId = await CreateBoardAsync();
        var columnId = await CreateColumnAsync(boardId);
        var cardResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns/{columnId}/cards", new
        {
            Title = "Install PLC",
            GanttTaskId = (Guid?)null
        });
        var cardId = await cardResponse.Content.ReadFromJsonAsync<Guid>();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = "board-assignee-1@example.com",
            DisplayName = "Card Assignee",
            Password = AuthTestHelper.DefaultPassword
        });
        var assigneeUserId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PatchAsJsonAsync(
            $"/api/boards/{boardId}/columns/{columnId}/cards/{cardId}/assignee", new { UserId = assigneeUserId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PatchAssignee_WithUserInAnotherOrganization_ReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "board-owner-6@example.com"));
        var boardId = await CreateBoardAsync();
        var columnId = await CreateColumnAsync(boardId);
        var cardResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns/{columnId}/cards", new
        {
            Title = "Install PLC",
            GanttTaskId = (Guid?)null
        });
        var cardId = await cardResponse.Content.ReadFromJsonAsync<Guid>();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "board-outsider@example.com",
            DisplayName = "Outsider",
            Password = AuthTestHelper.DefaultPassword
        });
        var outsiderUserId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var ownerToken = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "board-owner-6b@example.com");
        UseToken(ownerToken);

        var response = await _client.PatchAsJsonAsync(
            $"/api/boards/{boardId}/columns/{columnId}/cards/{cardId}/assignee", new { UserId = outsiderUserId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
