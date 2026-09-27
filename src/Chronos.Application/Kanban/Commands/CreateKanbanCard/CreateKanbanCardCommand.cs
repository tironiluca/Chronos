using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateKanbanCard;

public record CreateKanbanCardCommand(Guid CallerOrganizationId, Guid BoardId, Guid ColumnId, string Title, Guid? GanttTaskId) : IRequest<Result<Guid>>;
