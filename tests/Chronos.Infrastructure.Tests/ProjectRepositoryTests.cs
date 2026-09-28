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

    // Regression test: adding a task to a Project reloaded from the DB (rather than one just
    // constructed) previously threw DbUpdateConcurrencyException -- see TrackNewTask.
    [Fact]
    public async Task AddTask_ToReloadedProject_ThenSaveChanges_Persists()
    {
        var repository = new ProjectRepository(_context);
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        await repository.AddAsync(project);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(project.Id);
        var task = reloaded!.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        repository.TrackNewTask(task);

        await repository.SaveChangesAsync();

        var reloadedAgain = await repository.GetByIdAsync(project.Id);
        reloadedAgain!.Tasks.Should().ContainSingle(t => t.Name == "Install PLC");
    }

    // Regression coverage for the resource-availability query (see IMPLEMENTATION_PLAN.md), which
    // needs every project's tasks loaded across a whole organization in one call.
    [Fact]
    public async Task GetDetailedByOrganizationAsync_EagerLoadsTasksAcrossAllProjectsInTheOrganization()
    {
        var organizationId = Guid.NewGuid();
        var repository = new ProjectRepository(_context);
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        await repository.AddAsync(project);
        await repository.AddAsync(new Project(Guid.NewGuid(), "Other Org Project", "OOP", new DateOnly(2026, 1, 1)));
        await repository.SaveChangesAsync();

        var found = await repository.GetDetailedByOrganizationAsync(organizationId);

        found.Should().ContainSingle(p => p.Id == project.Id)
            .Which.Tasks.Should().ContainSingle(t => t.Name == "Install PLC");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
