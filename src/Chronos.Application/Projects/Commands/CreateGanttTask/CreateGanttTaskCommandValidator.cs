using FluentValidation;

namespace Chronos.Application.Projects.Commands.CreateGanttTask;

public class CreateGanttTaskCommandValidator : AbstractValidator<CreateGanttTaskCommand>
{
    public CreateGanttTaskCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot precede start date.");
    }
}
