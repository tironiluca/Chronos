using FluentValidation;

namespace Chronos.Application.Kanban.Commands.CreateKanbanColumn;

public class CreateKanbanColumnCommandValidator : AbstractValidator<CreateKanbanColumnCommand>
{
    public CreateKanbanColumnCommandValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
