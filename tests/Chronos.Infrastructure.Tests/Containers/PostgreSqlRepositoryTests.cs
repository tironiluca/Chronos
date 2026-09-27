using Chronos.Domain.Leave;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests.Containers;

// Same round trips as SqlServerRepositoryTests, run against a real PostgreSQL container --
// the point is proving the abstraction holds across providers, not proving each provider's
// SQL dialect works in isolation, so keeping the two test classes near-identical is deliberate.
[Trait("Category", "Container")]
public class PostgreSqlRepositoryTests : IClassFixture<PostgreSqlContainerFixture>, IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private ChronosDbContext _context = default!;

    public PostgreSqlRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlChronosDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _context = new PostgreSqlChronosDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProjectRepository_AddAsync_ThenGetById_RoundTripsWithTasks_OnPostgreSql()
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
    public async Task LeaveRequestRepository_AddAsync_ThenGetById_RoundTrips_OnPostgreSql()
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
    public async Task UserRepository_GetByEmailAsync_IsCaseInsensitive_OnPostgreSql()
    {
        var repository = new UserRepository(_context);
        await repository.AddAsync(new User(Guid.NewGuid(), "Luca@Example.com", "Luca", "hash"));
        await repository.SaveChangesAsync();

        var found = await repository.GetByEmailAsync("luca@example.com");

        found.Should().NotBeNull();
    }
}
