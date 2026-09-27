using Chronos.Domain.Users;

namespace Chronos.Application.Users;

public interface IPasswordHistoryRepository
{
    // Most recent first, capped at `count` -- used to block reuse of recently-used passwords.
    Task<IReadOnlyList<PasswordHistory>> GetRecentAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordHistory entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
