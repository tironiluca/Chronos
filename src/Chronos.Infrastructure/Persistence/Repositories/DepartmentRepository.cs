using Chronos.Application.Organizations;
using Chronos.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ChronosDbContext _context;

    public DepartmentRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default) =>
        _context.Departments.FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Department>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _context.Departments
            .Where(d => d.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default) =>
        await _context.Departments.AddAsync(department, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
