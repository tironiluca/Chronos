using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Queries.GetLeaveRequestsByOrganization;

public record GetLeaveRequestsByOrganizationQuery(Guid OrganizationId) : IRequest<Result<IReadOnlyList<LeaveRequestDto>>>;
