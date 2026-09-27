using Chronos.Domain.Leave;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests.Containers;

// Proves the multi-DB abstraction actually holds on SQL Server, not just on the fast Sqlite
// tests elsewhere in this project. One real container per test class (shared across its tests);
// each test uses fresh Guids so sharing the container's database between tests is safe.
[Trait("Category", "Container")]
public class SqlServerRepositoryTests : IClassFixture<SqlServerContainerFixture>, IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;
    private ChronosDbContext _context = default!;

    public SqlServerRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SqlServerChronosDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _context = new SqlServerChronosDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProjectRepository_AddAsync_ThenGetById_RoundTripsWithTasks_OnSqlServer()
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

    [Fact]
    public async Task LeaveRequestRepository_AddAsync_ThenGetById_RoundTrips_OnSqlServer()
    {
        var repository = new LeaveRequestRepository(_context);
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));

        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(leaveRequest.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(LeaveStatus.Pending);
    }

    [Fact]
    public async Task UserRepository_GetByEmailAsync_IsCaseInsensitive_OnSqlServer()
    {
        var repository = new UserRepository(_context);
        await repository.AddAsync(new User(Guid.NewGuid(), "Luca@Example.com", "Luca", "hash"));
        await repository.SaveChangesAsync();

        var found = await repository.GetByEmailAsync("luca@example.com");

        found.Should().NotBeNull();
    }
}
