using FluentValidation;

namespace Chronos.Application.Kanban.Commands.AssignKanbanCardUser;

public class AssignKanbanCardUserCommandValidator : AbstractValidator<AssignKanbanCardUserCommand>
{
    public AssignKanbanCardUserCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
    }
}
