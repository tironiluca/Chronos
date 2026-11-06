using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.RemoveGanttTaskDependency;

public class RemoveGanttTaskDependencyCommandHandler : IRequestHandler<RemoveGanttTaskDependencyCommand, Result>
{
    private readonly IProjectRepository _projectRepository;

    public RemoveGanttTaskDependencyCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result> Handle(RemoveGanttTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Project '{request.ProjectId}' was not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == request.TaskId);
        if (task is null)
            return Result.Failure($"Task '{request.TaskId}' was not found.");

        try
        {
            task.RemoveDependency(request.PredecessorTaskId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
