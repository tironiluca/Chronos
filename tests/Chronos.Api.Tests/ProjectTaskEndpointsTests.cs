using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class ProjectTaskEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProjectTaskEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    private async Task<Guid> AuthenticateAndCreateProjectAsync(Guid organizationId, string email)
    {
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Line 3 Upgrade",
            Code = "L3U",
            StartDate = new DateOnly(2026, 1, 1)
        });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task PostTask_WithValidPayload_ReturnsCreated()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-owner-1@example.com");

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostTask_ForProjectInAnotherOrganization_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-owner-2@example.com");

        // Switch to a different org's user.
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "task-intruder@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTasks_ReturnsTasksCreatedForTheProject()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-owner-3@example.com");
        await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });

        var response = await _client.GetAsync($"/api/projects/{projectId}/tasks");
        var body = await response.Content.ReadFromJsonAsync<List<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle();
    }

    [Fact]
    public async Task PatchAssignee_WithUserInSameOrganization_ReturnsNoContent()
    {
        var organizationId = Guid.NewGuid();
        var projectId = await AuthenticateAndCreateProjectAsync(organizationId, "task-owner-4@example.com");
        var taskResponse = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = "task-assignee-1@example.com",
            DisplayName = "Task Assignee",
            Password = AuthTestHelper.DefaultPassword
        });
        var assigneeUserId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/assignee", new { UserId = assigneeUserId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PatchAssignee_WithUserInAnotherOrganization_ReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        var projectId = await AuthenticateAndCreateProjectAsync(organizationId, "task-owner-5@example.com");
        var taskResponse = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "task-outsider@example.com",
            DisplayName = "Task Outsider",
            Password = AuthTestHelper.DefaultPassword
        });
        var outsiderUserId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var ownerToken = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "task-owner-5b@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/assignee", new { UserId = outsiderUserId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
