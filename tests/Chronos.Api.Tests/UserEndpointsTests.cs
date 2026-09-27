using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class UserEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task ChangeRole_AsAdmin_PromotesUserInSameOrganization()
    {
        var organizationId = Guid.NewGuid();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = "promote-me@example.com",
            DisplayName = "Promote Me",
            Password = AuthTestHelper.DefaultPassword
        });
        var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "admin-1@example.com"));

        var response = await _client.PatchAsJsonAsync($"/api/users/{userId}/role", new { Role = "Approver" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeRole_AsPlainEmployee_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "employee-4@example.com"));

        var response = await _client.PatchAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", new { Role = "Admin" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_AsAdmin_ForUserInAnotherOrganization_ReturnsBadRequest()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "other-org-user@example.com",
            DisplayName = "Other Org User",
            Password = AuthTestHelper.DefaultPassword
        });
        var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, Guid.NewGuid(), "admin-2@example.com"));

        var response = await _client.PatchAsJsonAsync($"/api/users/{userId}/role", new { Role = "Admin" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
