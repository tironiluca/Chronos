using Chronos.Domain.Projects;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests;

// Fast, provider-agnostic tests run against Sqlite in-memory on every build.
// SqlServer/PostgreSql-specific behaviour (concurrency tokens, provider SQL functions) belongs in
// separate Testcontainers-backed fixtures -- see /docs or CI workflow -- run in CI only (needs Docker).
public class ProjectRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChronosDbContext _context;

    public ProjectRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChronosDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ChronosDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsProjectWithTasks()
    {
        var repository = new ProjectRepository(_context);
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        await repository.AddAsync(project);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(project.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Tasks.Should().ContainSingle(t => t.Name == "Install PLC");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
