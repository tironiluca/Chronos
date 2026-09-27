using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Queries.GetLeaveRequestsByOrganization;

public class GetLeaveRequestsByOrganizationQueryHandler
    : IRequestHandler<GetLeaveRequestsByOrganizationQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetLeaveRequestsByOrganizationQueryHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(
        GetLeaveRequestsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var leaveRequests = await _leaveRequestRepository.GetByOrganizationAsync(request.OrganizationId, cancellationToken);

        var dtos = leaveRequests
            .Select(l => new LeaveRequestDto(
                l.Id,
                l.OrganizationId,
                l.RequesterId,
                l.Type.ToString(),
                l.StartDate,
                l.EndDate,
                l.Status.ToString(),
                l.ApproverId,
                l.RejectionReason))
            .ToList();

        return Result.Success<IReadOnlyList<LeaveRequestDto>>(dtos);
    }
}
