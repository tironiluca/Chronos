using FluentValidation;

namespace Chronos.Application.Kanban.Commands.MoveKanbanCard;

public class MoveKanbanCardCommandValidator : AbstractValidator<MoveKanbanCardCommand>
{
    public MoveKanbanCardCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TargetColumnId).NotEmpty();
    }
}
