using Chronos.Domain.Users;

namespace Chronos.Application.Users;

public interface IRightRepository
{
    Task<IReadOnlyList<Right>> GetRightsForRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
