using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chronos.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.Api.Tests;

public class DepartmentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DepartmentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithIsolatedSqlite();
        _client = _factory.CreateClient();
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task PostDepartment_AsAdmin_ReturnsCreated()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "admin-dept-1@example.com"));

        var response = await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering", Code = "ENG" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostDepartment_AsPlainEmployee_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.RegisterAndLoginAsync(_client, organizationId, "employee-dept-1@example.com"));

        var response = await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering", Code = "ENG" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostDepartment_WithDuplicateCodeInSameOrganization_ReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "admin-dept-2@example.com"));
        await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering", Code = "ENG" });

        var second = await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering Two", Code = "eng" });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostDepartment_WithNestedParent_ReturnsCreated()
    {
        var organizationId = Guid.NewGuid();
        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, organizationId, "admin-dept-3@example.com"));
        var parentResponse = await _client.PostAsJsonAsync("/api/departments", new { Name = "Engineering", Code = "ENG" });
        var parentId = await parentResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PostAsJsonAsync("/api/departments", new
        {
            Name = "Platform",
            Code = "PLAT",
            ParentDepartmentId = parentId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetDepartments_OnlyReturnsTheCallersOwnOrganization()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, orgA, "admin-dept-a@example.com"));
        await _client.PostAsJsonAsync("/api/departments", new { Name = "Org A Dept", Code = "A1" });

        UseToken(await AuthTestHelper.SeedAndLoginAsAdminAsync(_factory, _client, orgB, "admin-dept-b@example.com"));
        await _client.PostAsJsonAsync("/api/departments", new { Name = "Org B Dept", Code = "B1" });

        var response = await _client.GetAsync("/api/departments");
        var body = await response.Content.ReadFromJsonAsync<List<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().ContainSingle(); // only orgB's department, since the client is now logged in as orgB's admin
    }
}
