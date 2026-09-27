using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class ProjectEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProjectEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // Development environment wires the Sqlite connection string from appsettings.Development.json
        // so this test needs neither SQL Server nor Docker.
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task PostProject_WithValidPayload_ReturnsCreated()
    {
        var payload = new
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Line 3 Upgrade",
            Code = "L3U",
            StartDate = new DateOnly(2026, 1, 1)
        };

        var response = await _client.PostAsJsonAsync("/api/projects", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
