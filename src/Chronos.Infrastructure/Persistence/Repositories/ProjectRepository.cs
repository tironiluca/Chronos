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

    // GanttTask.Id is already a non-default Guid by the time EF's change tracker discovers it
    // (Entity sets Id = Guid.NewGuid() at construction, not on save) -- when a task is added to
    // an already-tracked (Unchanged) Project's owned collection rather than reached via a newly
    // Added parent, EF's default heuristic for graph-discovered entities assumes a non-default
    // key means "already exists" and marks it Modified instead of Added, producing a 0-row
    // DbUpdateConcurrencyException on save (it issues an UPDATE, not an INSERT). Explicitly
    // marking it Added sidesteps that heuristic. Not needed when the Project itself is also new
    // (AddAsync above) -- Added cascades to owned children automatically in that case.
    public void TrackNewTask(GanttTask task) => _context.Entry(task).State = EntityState.Added;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
