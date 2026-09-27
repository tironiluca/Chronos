using Chronos.Application.Leave;
using Chronos.Domain.Leave;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly ChronosDbContext _context;

    public LeaveRequestRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _context.LeaveRequests
            .Where(l => l.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default) =>
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
