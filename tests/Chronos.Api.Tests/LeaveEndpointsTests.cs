using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class LeaveEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LeaveEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task PostLeaveRequest_WithValidPayload_ReturnsCreated()
    {
        var payload = new
        {
            OrganizationId = Guid.NewGuid(),
            RequesterId = Guid.NewGuid(),
            Type = "Vacation",
            StartDate = new DateOnly(2026, 8, 10),
            EndDate = new DateOnly(2026, 8, 17)
        };

        var response = await _client.PostAsJsonAsync("/api/leave-requests", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ApproveThenApproveAgain_SecondCallReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            OrganizationId = Guid.NewGuid(),
            RequesterId = Guid.NewGuid(),
            Type = "SickLeave",
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 3)
        });
        var leaveRequestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var firstApprove = await _client.PostAsJsonAsync(
            $"/api/leave-requests/{leaveRequestId}/approve", new { ApproverId = Guid.NewGuid() });
        var secondApprove = await _client.PostAsJsonAsync(
            $"/api/leave-requests/{leaveRequestId}/approve", new { ApproverId = Guid.NewGuid() });

        firstApprove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLeaveRequests_ByOrganization_ReturnsOnlyThatOrganizationsRequests()
    {
        var organizationId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            OrganizationId = organizationId,
            RequesterId = Guid.NewGuid(),
            Type = "Unpaid",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 2)
        });

        var response = await _client.GetAsync($"/api/leave-requests?organizationId={organizationId}");
        var body = await response.Content.ReadFromJsonAsync<List<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle();
    }
}
