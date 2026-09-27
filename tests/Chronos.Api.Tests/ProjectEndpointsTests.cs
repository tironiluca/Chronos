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
}
