using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Application.Common;
using Chronos.Domain.Users;
using Chronos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Api.Tests.TestSupport;

public static class AuthTestHelper
{
    public const string DefaultPassword = "TestPassword123";

    public static async Task<string> RegisterAndLoginAsync(HttpClient client, Guid organizationId, string email = "employee@example.com")
    {
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            OrganizationId = organizationId,
            Email = email,
            DisplayName = "Test Employee",
            Password = DefaultPassword
        });

        return await LoginAsync(client, email);
    }

    // Registration always creates an Employee (see RegisterUserCommandHandler) -- an
    // Approver/Admin is seeded directly here, bypassing the command, purely for test setup.
    public static async Task<string> SeedAndLoginAsApproverAsync(
        WebApplicationFactory<Program> factory, HttpClient client, Guid organizationId, string email = "approver@example.com")
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        dbContext.Users.Add(new User(organizationId, email, "Test Approver", hasher.Hash(DefaultPassword), UserRole.Approver));
        await dbContext.SaveChangesAsync();

        return await LoginAsync(client, email);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = DefaultPassword });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }
}
