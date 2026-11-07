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

    // The "TrackNewTask-class bug, one level deeper" check from IMPLEMENTATION_PLAN.md:
    // TaskDependency is OwnsMany'd under GanttTask (itself OwnsMany'd under Project). Asserted
    // through a fresh DbContext so identity resolution can't mask a write that never happened.
    [Fact]
    public async Task AddTaskDependency_ToReloadedProject_ThenSaveChanges_Persists()
    {
        var repository = new ProjectRepository(_context);
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        await repository.AddAsync(project);
        await repository.SaveChangesAsync();

        using (var editContext = NewContext())
        {
            var editRepository = new ProjectRepository(editContext);
            var reloaded = await editRepository.GetByIdAsync(project.Id);
            reloaded!.AddTaskDependency(build.Id, design.Id, DependencyType.FinishToStart, 2);
            await editRepository.SaveChangesAsync();
        }

        using var verifyContext = NewContext();
        var persisted = await new ProjectRepository(verifyContext).GetByIdAsync(project.Id);
        var dependency = persisted!.Tasks.Single(t => t.Id == build.Id).Dependencies.Should().ContainSingle().Subject;
        dependency.PredecessorTaskId.Should().Be(design.Id);
        dependency.Type.Should().Be(DependencyType.FinishToStart);
        dependency.LagDays.Should().Be(2);
    }

    [Fact]
    public async Task RescheduleAndUpdateProgress_OnReloadedProject_ThenSaveChanges_Persists()
    {
        var repository = new ProjectRepository(_context);
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        await repository.AddAsync(project);
        await repository.SaveChangesAsync();

        using (var editContext = NewContext())
        {
            var editRepository = new ProjectRepository(editContext);
            var reloadedTask = (await editRepository.GetByIdAsync(project.Id))!.Tasks.Single();
            reloadedTask.Reschedule(new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6));
            reloadedTask.UpdateProgress(40);
            await editRepository.SaveChangesAsync();
        }

        using var verifyContext = NewContext();
        var persisted = (await new ProjectRepository(verifyContext).GetByIdAsync(project.Id))!.Tasks.Single();
        persisted.StartDate.Should().Be(new DateOnly(2026, 2, 2));
        persisted.EndDate.Should().Be(new DateOnly(2026, 2, 6));
        persisted.ProgressPercent.Should().Be(40);
    }

    private ChronosDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ChronosDbContext>().UseSqlite(_connection).Options);

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
