using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Queries.GetBoardById;

public record GetBoardByIdQuery(Guid CallerOrganizationId, Guid BoardId) : IRequest<Result<BoardDetailDto>>;
