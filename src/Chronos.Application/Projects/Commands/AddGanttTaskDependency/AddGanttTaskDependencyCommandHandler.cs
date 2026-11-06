using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.AddGanttTaskDependency;

public class AddGanttTaskDependencyCommandHandler : IRequestHandler<AddGanttTaskDependencyCommand, Result>
{
    private readonly IProjectRepository _projectRepository;

    public AddGanttTaskDependencyCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result> Handle(AddGanttTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Project '{request.ProjectId}' was not found.");

        try
        {
            // Project owns the graph-level checks (both tasks in this project, no cycle); the
            // task owns self-reference/duplicate checks.
            project.AddTaskDependency(request.TaskId, request.PredecessorTaskId, request.Type, request.LagDays);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        // No TrackNew* call needed, unlike tasks/columns/cards: TaskDependency's key is an EF
        // shadow property left at default until save, so EF classifies it as Added on its own.
        // Covered by ProjectRepositoryTests.AddTaskDependency_ToReloadedProject_ThenSaveChanges_Persists.
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
