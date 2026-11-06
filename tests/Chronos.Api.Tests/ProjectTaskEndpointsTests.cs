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

    private async Task<Guid> CreateTaskAsync(Guid projectId, string name, DateOnly start, DateOnly end) =>
        await (await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { Name = name, StartDate = start, EndDate = end }))
            .Content.ReadFromJsonAsync<Guid>();

    private async Task<System.Text.Json.JsonElement> GetTaskAsync(Guid projectId, Guid taskId)
    {
        var tasks = await _client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>($"/api/projects/{projectId}/tasks");
        return tasks!.Single(t => t.GetProperty("id").GetGuid() == taskId);
    }

    [Fact]
    public async Task PatchSchedule_ThenPatchProgress_PersistsBoth()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-1@example.com");
        var taskId = await CreateTaskAsync(projectId, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        var schedule = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/schedule", new
        {
            StartDate = new DateOnly(2026, 2, 2),
            EndDate = new DateOnly(2026, 2, 6)
        });
        var progress = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/progress", new { ProgressPercent = 60 });

        schedule.StatusCode.Should().Be(HttpStatusCode.NoContent);
        progress.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var task = await GetTaskAsync(projectId, taskId);
        task.GetProperty("startDate").GetString().Should().Be("2026-02-02");
        task.GetProperty("endDate").GetString().Should().Be("2026-02-06");
        task.GetProperty("progressPercent").GetInt32().Should().Be(60);
    }

    [Fact]
    public async Task PatchSchedule_WithEndBeforeStart_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-2@example.com");
        var taskId = await CreateTaskAsync(projectId, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        var response = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/schedule", new
        {
            StartDate = new DateOnly(2026, 2, 6),
            EndDate = new DateOnly(2026, 2, 2)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchProgress_OutOfRange_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-3@example.com");
        var taskId = await CreateTaskAsync(projectId, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        var response = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/progress", new { ProgressPercent = 101 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchSchedule_ForProjectInAnotherOrganization_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-4@example.com");
        var taskId = await CreateTaskAsync(projectId, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "task-editor-intruder@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/schedule", new
        {
            StartDate = new DateOnly(2026, 2, 2),
            EndDate = new DateOnly(2026, 2, 6)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostDependency_ThenDelete_RoundTripsThroughTaskDependencies()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-5@example.com");
        var design = await CreateTaskAsync(projectId, "Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = await CreateTaskAsync(projectId, "Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));

        var added = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks/{build}/dependencies", new
        {
            PredecessorTaskId = design,
            Type = "FinishToStart",
            LagDays = 1
        });

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        (await GetTaskAsync(projectId, build)).GetProperty("dependencies").EnumerateArray()
            .Select(d => d.GetGuid()).Should().Equal(design);

        var removed = await _client.DeleteAsync($"/api/projects/{projectId}/tasks/{build}/dependencies/{design}");

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetTaskAsync(projectId, build)).GetProperty("dependencies").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PostDependency_ThatWouldCreateACycle_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-6@example.com");
        var design = await CreateTaskAsync(projectId, "Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = await CreateTaskAsync(projectId, "Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks/{build}/dependencies", new { PredecessorTaskId = design });

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks/{design}/dependencies", new { PredecessorTaskId = build });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("cycle");
    }

    [Fact]
    public async Task PostDependency_OnPredecessorFromAnotherProject_ReturnsBadRequest()
    {
        var projectId = await AuthenticateAndCreateProjectAsync(Guid.NewGuid(), "task-editor-7@example.com");
        var build = await CreateTaskAsync(projectId, "Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        var otherProjectId = await (await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Line 4", Code = "L4", StartDate = new DateOnly(2026, 1, 1)
        })).Content.ReadFromJsonAsync<Guid>();
        var foreignTask = await CreateTaskAsync(otherProjectId, "Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks/{build}/dependencies", new { PredecessorTaskId = foreignTask });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
