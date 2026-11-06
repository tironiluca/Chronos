using FluentValidation;

namespace Chronos.Application.Projects.Commands.RemoveGanttTaskDependency;

public class RemoveGanttTaskDependencyCommandValidator : AbstractValidator<RemoveGanttTaskDependencyCommand>
{
    public RemoveGanttTaskDependencyCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.PredecessorTaskId).NotEmpty();
    }
}
