using FluentValidation;

namespace Chronos.Application.Projects.Commands.UpdateGanttTaskProgress;

public class UpdateGanttTaskProgressCommandValidator : AbstractValidator<UpdateGanttTaskProgressCommand>
{
    public UpdateGanttTaskProgressCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ProgressPercent).InclusiveBetween(0, 100);
    }
}
