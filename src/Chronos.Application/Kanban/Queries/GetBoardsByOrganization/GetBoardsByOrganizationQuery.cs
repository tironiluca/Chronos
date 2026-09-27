using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Queries.GetBoardsByOrganization;

public record GetBoardsByOrganizationQuery(Guid CallerOrganizationId) : IRequest<Result<IReadOnlyList<BoardDto>>>;
