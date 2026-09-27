using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateBoard;

public record CreateBoardCommand(Guid CallerOrganizationId, string Name, Guid? ProjectId) : IRequest<Result<Guid>>;
