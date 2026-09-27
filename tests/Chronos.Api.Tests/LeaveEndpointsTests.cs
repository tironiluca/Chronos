using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class LeaveEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LeaveEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        _client = _factory.CreateClient();
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task PostLeaveRequest_WithValidPayload_ReturnsCreated()
    {
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, Guid.NewGuid(), "employee-1@example.com"));

        var response = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "Vacation",
            StartDate = new DateOnly(2026, 8, 10),
            EndDate = new DateOnly(2026, 8, 17)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Approve_AsPlainEmployee_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "employee-2@example.com"));
        var createResponse = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "SickLeave",
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 3)
        });
        var leaveRequestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var approveResponse = await _client.PostAsync($"/api/leave-requests/{leaveRequestId}/approve", null);

        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveThenApproveAgain_SecondCallReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "employee-3@example.com"));
        var createResponse = await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "SickLeave",
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 3)
        });
        var leaveRequestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.SeedAndLoginAsApproverAsync(_factory, _client, organizationId, "approver-1@example.com"));

        var firstApprove = await _client.PostAsync($"/api/leave-requests/{leaveRequestId}/approve", null);
        var secondApprove = await _client.PostAsync($"/api/leave-requests/{leaveRequestId}/approve", null);

        firstApprove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLeaveRequests_OnlyReturnsTheCallersOwnOrganization()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, orgA, "org-a-user@example.com"));
        await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "Unpaid",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 2)
        });

        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, orgB, "org-b-user@example.com"));
        await _client.PostAsJsonAsync("/api/leave-requests", new
        {
            Type = "Unpaid",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 2)
        });

        var response = await _client.GetAsync("/api/leave-requests");
        var body = await response.Content.ReadFromJsonAsync<List<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle(); // only orgB's request, since the client is now logged in as orgB's user
    }
}
