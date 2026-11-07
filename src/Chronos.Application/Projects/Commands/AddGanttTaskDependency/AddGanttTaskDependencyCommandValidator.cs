using FluentValidation;

namespace Chronos.Application.Projects.Commands.AddGanttTaskDependency;

public class AddGanttTaskDependencyCommandValidator : AbstractValidator<AddGanttTaskDependencyCommand>
{
    public AddGanttTaskDependencyCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.PredecessorTaskId).NotEmpty()
            .NotEqual(x => x.TaskId).WithMessage("A task cannot depend on itself.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.LagDays).InclusiveBetween(-365, 365);
    }
}
