using Chronos.Application.Projects;
using Chronos.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ChronosDbContext _context;

    public ProjectRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _context.Projects
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default) =>
        await _context.Projects.AddAsync(project, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
