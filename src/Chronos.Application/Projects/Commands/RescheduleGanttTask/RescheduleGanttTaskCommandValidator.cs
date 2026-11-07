using FluentValidation;

namespace Chronos.Application.Projects.Commands.RescheduleGanttTask;

public class RescheduleGanttTaskCommandValidator : AbstractValidator<RescheduleGanttTaskCommand>
{
    public RescheduleGanttTaskCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot precede start date.");
    }
}
