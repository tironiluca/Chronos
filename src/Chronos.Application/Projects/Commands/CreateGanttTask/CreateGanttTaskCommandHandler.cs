using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.CreateGanttTask;

public class CreateGanttTaskCommandHandler : IRequestHandler<CreateGanttTaskCommand, Result<Guid>>
{
    private readonly IProjectRepository _projectRepository;

    public CreateGanttTaskCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<Guid>> Handle(CreateGanttTaskCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        // Same failure for "not found" and "belongs to another organization" -- mirrors
        // PromoteUserRoleCommandHandler: never let the response reveal whether a project id
        // exists in someone else's org.
        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<Guid>($"Project '{request.ProjectId}' was not found.");

        var task = project.AddTask(request.Name, request.StartDate, request.EndDate, request.ParentTaskId);
        _projectRepository.TrackNewTask(task);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(task.Id);
    }
}
