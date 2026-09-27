using Chronos.Application.Users;
using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly ChronosDbContext _context;

    public PasswordHistoryRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PasswordHistory>> GetRecentAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        await _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PasswordHistory entry, CancellationToken cancellationToken = default) =>
        await _context.PasswordHistories.AddAsync(entry, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
