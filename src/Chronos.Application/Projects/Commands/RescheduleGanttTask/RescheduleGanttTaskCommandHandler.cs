using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.RescheduleGanttTask;

// Dependencies are not enforced against the new dates (a FinishToStart successor may end up
// starting before its predecessor ends) -- they're informational today, same as the renderer.
public class RescheduleGanttTaskCommandHandler : IRequestHandler<RescheduleGanttTaskCommand, Result>
{
    private readonly IProjectRepository _projectRepository;

    public RescheduleGanttTaskCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result> Handle(RescheduleGanttTaskCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Project '{request.ProjectId}' was not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == request.TaskId);
        if (task is null)
            return Result.Failure($"Task '{request.TaskId}' was not found.");

        try
        {
            task.Reschedule(request.StartDate, request.EndDate);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
