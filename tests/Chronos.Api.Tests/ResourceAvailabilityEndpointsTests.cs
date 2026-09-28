using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class ResourceAvailabilityEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ResourceAvailabilityEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<(Guid UserId, string Token)> RegisterAsync(Guid organizationId, string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = email,
            DisplayName = "Availability Test User",
            Password = AuthTestHelper.DefaultPassword
        });
        var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = AuthTestHelper.DefaultPassword });
        var json = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return (userId, doc.RootElement.GetProperty("token").GetString()!);
    }

    private static string AvailabilityUrl(DateOnly from, DateOnly to, Guid? departmentId = null, Guid? projectId = null)
    {
        var url = $"/api/resources/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (departmentId is { } d) url += $"&departmentId={d}";
        if (projectId is { } p) url += $"&projectId={p}";
        return url;
    }

    [Fact]
    public async Task GetAvailability_IncludesApprovedLeaveOverlappingRange()
    {
        var organizationId = Guid.NewGuid();
        var (_, employeeToken) = await RegisterAsync(organizationId, "avail-employee-1@example.com");
        UseToken(employeeToken);
        var leaveResponse = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "Vacation",
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 5)
        });
        var leaveRequestId = await leaveResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.SeedAndLoginAsApproverAsync(_factory, _client, organizationId, "avail-approver-1@example.com"));
        await _client.PostAsync($"/api/leave-requests/{leaveRequestId}/approve", null);

        var response = await _client.GetAsync(AvailabilityUrl(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain(leaveRequestId.ToString());
    }

    [Fact]
    public async Task GetAvailability_ExcludesPendingLeave()
    {
        var organizationId = Guid.NewGuid();
        var (_, employeeToken) = await RegisterAsync(organizationId, "avail-employee-2@example.com");
        UseToken(employeeToken);
        var leaveResponse = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "Vacation",
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 5)
        });
        var leaveRequestId = await leaveResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync(AvailabilityUrl(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().NotContain(leaveRequestId.ToString());
    }

    [Fact]
    public async Task GetAvailability_IncludesAssignedGanttTaskOverlappingRange()
    {
        var organizationId = Guid.NewGuid();
        var (userId, token) = await RegisterAsync(organizationId, "avail-employee-3@example.com");
        UseToken(token);
        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Line 3 Upgrade",
            Code = "L3U",
            StartDate = new DateOnly(2026, 1, 1)
        });
        var projectId = await projectResponse.Content.ReadFromJsonAsync<Guid>();
        var taskResponse = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            Name = "Install PLC",
            StartDate = new DateOnly(2026, 3, 5),
            EndDate = new DateOnly(2026, 3, 10)
        });
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();
        await _client.PatchAsJsonAsync($"/api/projects/{projectId}/tasks/{taskId}/assignee", new { UserId = userId });

        var response = await _client.GetAsync(AvailabilityUrl(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("Install PLC");
    }

    [Fact]
    public async Task GetAvailability_WithDepartmentId_OnlyReturnsUsersInThatDepartment()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "avail-admin-1@example.com"));
        var departmentResponse = await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering", Code = "ENG" });
        var departmentId = await departmentResponse.Content.ReadFromJsonAsync<Guid>();

        var (inDeptUserId, _) = await RegisterAsync(organizationId, "avail-in-dept@example.com");
        var (outOfDeptUserId, _) = await RegisterAsync(organizationId, "avail-out-of-dept@example.com");

        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "avail-admin-2@example.com"));
        await _client.PatchAsJsonAsync($"/api/users/{inDeptUserId}/department", new { DepartmentId = departmentId });

        var response = await _client.GetAsync(AvailabilityUrl(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), departmentId: departmentId));
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain(inDeptUserId.ToString());
        json.Should().NotContain(outOfDeptUserId.ToString());
    }

    [Fact]
    public async Task GetAvailability_WithDepartmentInAnotherOrganization_ReturnsBadRequest()
    {
        var otherOrgId = Guid.NewGuid();
        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, otherOrgId, "avail-admin-3@example.com"));
        var otherOrgDepartmentResponse = await _client.PostAsJsonAsync("/api/departments", new { Name = "Other Org Dept", Code = "OOD" });
        var otherOrgDepartmentId = await otherOrgDepartmentResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "avail-employee-4@example.com"));

        var response = await _client.GetAsync(
            AvailabilityUrl(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), departmentId: otherOrgDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailability_WithProjectInAnotherOrganization_ReturnsBadRequest()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "avail-employee-5@example.com"));
        var otherOrgProjectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Other Org Project",
            Code = "OOP",
            StartDate = new DateOnly(2026, 1, 1)
        });
        var otherOrgProjectId = await otherOrgProjectResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "avail-employee-6@example.com"));

        var response = await _client.GetAsync(
            AvailabilityUrl(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), projectId: otherOrgProjectId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailability_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "avail-employee-7@example.com"));

        var response = await _client.GetAsync(AvailabilityUrl(new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
