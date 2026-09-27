using Chronos.Domain.Organizations;

namespace Chronos.Application.Organizations;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);

    // Loads every department in the organization -- used to resolve a department's descendants
    // for the availability query (see IMPLEMENTATION_PLAN.md); department counts per organization
    // are small enough that walking the tree in memory is simpler than a recursive SQL query.
    Task<IReadOnlyList<Department>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(Department department, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
