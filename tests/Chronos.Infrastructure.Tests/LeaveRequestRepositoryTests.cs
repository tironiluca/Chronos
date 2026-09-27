using Chronos.Domain.Leave;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests;

public class LeaveRequestRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChronosDbContext _context;

    public LeaveRequestRepositoryTests()
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
    public async Task AddAsync_ThenSaveChanges_PersistsLeaveRequest()
    {
        var repository = new LeaveRequestRepository(_context);
        var organizationId = Guid.NewGuid();
        var leaveRequest = new LeaveRequest(organizationId, Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));

        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(leaveRequest.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(LeaveStatus.Pending);
    }

    [Fact]
    public async Task GetByOrganizationAsync_ReturnsOnlyRequestsForThatOrganization()
    {
        var repository = new LeaveRequestRepository(_context);
        var targetOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        await repository.AddAsync(new LeaveRequest(targetOrg, Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17)));
        await repository.AddAsync(new LeaveRequest(otherOrg, Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5)));
        await repository.SaveChangesAsync();

        var result = await repository.GetByOrganizationAsync(targetOrg);

        result.Should().ContainSingle(l => l.OrganizationId == targetOrg);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
