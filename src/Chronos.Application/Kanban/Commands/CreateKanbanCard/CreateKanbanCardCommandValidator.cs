using FluentValidation;

namespace Chronos.Application.Kanban.Commands.CreateKanbanCard;

public class CreateKanbanCardCommandValidator : AbstractValidator<CreateKanbanCardCommand>
{
    public CreateKanbanCardCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
