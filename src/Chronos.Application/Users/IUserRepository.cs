using Chronos.Domain.Users;

namespace Chronos.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // Used by the resource-availability query (see IMPLEMENTATION_PLAN.md) to resolve which users
    // are in scope before filtering by department.
    Task<IReadOnlyList<User>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
