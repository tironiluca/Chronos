using FluentValidation;

namespace Chronos.Application.Projects.Commands.AssignGanttTaskUser;

public class AssignGanttTaskUserCommandValidator : AbstractValidator<AssignGanttTaskUserCommand>
{
    public AssignGanttTaskUserCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
