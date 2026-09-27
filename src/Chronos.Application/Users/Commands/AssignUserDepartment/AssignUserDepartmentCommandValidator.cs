using FluentValidation;

namespace Chronos.Application.Users.Commands.AssignUserDepartment;

public class AssignUserDepartmentCommandValidator : AbstractValidator<AssignUserDepartmentCommand>
{
    public AssignUserDepartmentCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
