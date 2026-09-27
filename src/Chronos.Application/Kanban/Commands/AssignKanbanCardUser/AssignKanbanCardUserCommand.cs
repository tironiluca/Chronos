using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.AssignKanbanCardUser;

// UserId null unassigns the card.
public record AssignKanbanCardUserCommand(Guid CallerOrganizationId, Guid BoardId, Guid ColumnId, Guid CardId, Guid? UserId) : IRequest<Result>;
