using FluentValidation;

namespace Chronos.Application.Users.Commands.PromoteUserRole;

public class PromoteUserRoleCommandValidator : AbstractValidator<PromoteUserRoleCommand>
{
    public PromoteUserRoleCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}
