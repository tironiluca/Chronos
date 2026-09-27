using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "new.user@example.com",
            DisplayName = "New User",
            Password = AuthTestHelper.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithAlreadyRegisteredEmail_ReturnsBadRequest()
    {
        var payload = new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "duplicate@example.com",
            DisplayName = "First",
            Password = AuthTestHelper.DefaultPassword
        };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var organizationId = Guid.NewGuid();
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "login-ok@example.com");

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = Guid.NewGuid(),
            Email = "wrong-pw@example.com",
            DisplayName = "Someone",
            Password = AuthTestHelper.DefaultPassword
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "wrong-pw@example.com", Password = "not-the-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/leave-requests");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Succeeds()
    {
        var organizationId = Guid.NewGuid();
        var token = await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "protected-access@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/leave-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
