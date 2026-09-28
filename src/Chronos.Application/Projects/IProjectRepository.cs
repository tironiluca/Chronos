using Chronos.Domain.Projects;

namespace Chronos.Application.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    // Same as GetByOrganizationAsync but eager-loads each Project's Tasks -- used by the
    // resource-availability query (see IMPLEMENTATION_PLAN.md), which needs every task across every
    // project in the organization, not just one project's.
    Task<IReadOnlyList<Project>> GetDetailedByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    // Call after adding a task to an already-persisted Project's Tasks collection (i.e. Project
    // was loaded via GetByIdAsync, not just constructed) -- see ProjectRepository for why this is
    // necessary despite GanttTask being reachable from a tracked Project.
    void TrackNewTask(GanttTask task);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
