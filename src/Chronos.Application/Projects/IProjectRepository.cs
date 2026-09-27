using Chronos.Domain.Projects;

namespace Chronos.Application.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    // Call after adding a task to an already-persisted Project's Tasks collection (i.e. Project
    // was loaded via GetByIdAsync, not just constructed) -- see ProjectRepository for why this is
    // necessary despite GanttTask being reachable from a tracked Project.
    void TrackNewTask(GanttTask task);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
