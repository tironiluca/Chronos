using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateKanbanColumn;

public record CreateKanbanColumnCommand(Guid CallerOrganizationId, Guid BoardId, string Name, int Order) : IRequest<Result<Guid>>;
