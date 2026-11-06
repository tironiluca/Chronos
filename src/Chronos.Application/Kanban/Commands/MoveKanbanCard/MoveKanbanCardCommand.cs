using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.MoveKanbanCard;

// ColumnId = where the card is now (route), TargetColumnId = where it goes. Both on BoardId.
public record MoveKanbanCardCommand(Guid CallerOrganizationId, Guid BoardId, Guid ColumnId, Guid CardId, Guid TargetColumnId) : IRequest<Result>;
