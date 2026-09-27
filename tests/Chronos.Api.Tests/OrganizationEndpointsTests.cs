using System.Net;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class OrganizationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrganizationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithIsolatedSqlite().CreateClient();
    }

    [Fact]
    public async Task Register_WithNewCode_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new
        {
            Name = "Sealed Air Rho",
            Code = "RHO"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateCode_ReturnsBadRequest()
    {
        var payload = new { Name = "Sealed Air Simpsonville", Code = "SIMP" };
        await _client.PostAsJsonAsync("/api/organizations", payload);

        var second = await _client.PostAsJsonAsync("/api/organizations", new { Name = "Different Name", Code = "simp" });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new { Name = "", Code = "EMPTY" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ThenRegisterUserAgainstReturnedId_ReturnsCreated()
    {
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations", new
        {
            Name = "Sealed Air Charlotte",
            Code = "CHR"
        });
        var organizationId = await orgResponse.Content.ReadFromJsonAsync<Guid>();

        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = "org-flow@example.com",
            DisplayName = "Org Flow",
            Password = AuthTestHelper.DefaultPassword
        });

        userResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
