using Chronos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronos.Api.Tests.TestSupport;

// Program.cs reads the Sqlite connection string eagerly, inside AddChronosInfrastructure, before
// builder.Build() runs -- so overriding it via WithWebHostBuilder(...).ConfigureAppConfiguration
// is too late: that config source is spliced in only once Build() is intercepted, well after the
// connection string was already captured. Overriding the DbContextOptions registration itself in
// ConfigureServices (the standard WebApplicationFactory pattern for swapping a test database)
// sidesteps that ordering problem entirely, and gives each factory its own fresh temp Sqlite
// file -- so test classes never collide with each other, or with a previous `dotnet test` run,
// on Program.cs's shared "chronos.db".
public static class IsolatedTestFactory
{
    public static WebApplicationFactory<Program> WithIsolatedSqlite(this WebApplicationFactory<Program> factory)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"chronos-tests-{Guid.NewGuid():N}.db");

        return factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ChronosDbContext>>();
                services.AddDbContext<ChronosDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            }));
    }
}
