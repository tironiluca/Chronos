using Chronos.Application.Users;
using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class RightRepository : IRightRepository
{
    private readonly ChronosDbContext _context;

    public RightRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Right>> GetRightsForRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var rightIds = await _context.RoleRights
            .Where(rr => rr.Role == role)
            .Select(rr => rr.RightId)
            .ToListAsync(cancellationToken);

        return await _context.Rights
            .Where(r => rightIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }
}
