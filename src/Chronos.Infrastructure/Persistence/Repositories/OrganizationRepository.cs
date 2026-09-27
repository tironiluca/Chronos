using Chronos.Application.Organizations;
using Chronos.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly ChronosDbContext _context;

    public OrganizationRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Organization?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Organizations.FirstOrDefaultAsync(o => o.Code == code, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default) =>
        await _context.Organizations.AddAsync(organization, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
