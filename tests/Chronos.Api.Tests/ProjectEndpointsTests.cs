using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class ProjectEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProjectEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // Development environment wires the Sqlite connection string, overridden to a fresh temp
        // file per factory (see IsolatedTestFactory) so this test needs neither SQL Server nor
        // Docker, and doesn't collide with the other Api.Tests classes on disk.
        _client = factory.WithIsolatedSqlite().CreateClient();
    }

    private async Task AuthenticateAsync(Guid organizationId, string email = "project-owner@example.com")
    {
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task PostProject_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Line 3 Upgrade",
            Code = "L3U",
            StartDate = new DateOnly(2026, 1, 1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostProject_WithValidPayload_ReturnsCreated()
    {
        await AuthenticateAsync(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Line 3 Upgrade",
            Code = "L3U",
            StartDate = new DateOnly(2026, 1, 1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private record ProjectResponse(Guid Id, string Name, string Code, string Status, List<TaskResponse> Tasks);
    private record TaskResponse(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, List<Guid> Dependencies);

    private async Task<Guid> CreateProjectAsync(string name, string code)
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { Name = name, Code = code, StartDate = new DateOnly(2026, 1, 1) });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyCallerOrganizationProjects_WithTasks()
    {
        await AuthenticateAsync(Guid.NewGuid(), "project-lister-other@example.com");
        await CreateProjectAsync("Other Org Project", "OOP");

        await AuthenticateAsync(Guid.NewGuid(), "project-lister@example.com");
        var projectId = await CreateProjectAsync("Line 3 Upgrade", "L3U");
        await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 10)
        });

        var response = await _client.GetAsync("/api/projects");
        var body = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle();
        body![0].Id.Should().Be(projectId);
        body[0].Status.Should().Be("Planned");
        body[0].Tasks.Should().ContainSingle(t => t.Name == "Install PLC" && t.Dependencies.Count == 0);
    }

    [Fact]
    public async Task GetProjects_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectById_InCallerOrganization_ReturnsProject()
    {
        await AuthenticateAsync(Guid.NewGuid(), "project-reader@example.com");
        var projectId = await CreateProjectAsync("Line 3 Upgrade", "L3U");

        var response = await _client.GetAsync($"/api/projects/{projectId}");
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be("L3U");
        body.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectById_InAnotherOrganization_ReturnsBadRequest()
    {
        await AuthenticateAsync(Guid.NewGuid(), "project-reader-owner@example.com");
        var projectId = await CreateProjectAsync("Line 3 Upgrade", "L3U");

        await AuthenticateAsync(Guid.NewGuid(), "project-reader-intruder@example.com");
        var response = await _client.GetAsync($"/api/projects/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
