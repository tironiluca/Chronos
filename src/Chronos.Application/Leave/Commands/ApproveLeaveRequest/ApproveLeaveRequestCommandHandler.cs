using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.ApproveLeaveRequest;

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public ApproveLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure($"Leave request '{request.LeaveRequestId}' was not found.");

        try
        {
            leaveRequest.Approve(request.ApproverId);
        }
        catch (InvalidOperationException ex)
        {
            // Domain invariant violation (e.g. not Pending) -- surfaced as a client error, not a 500.
            return Result.Failure(ex.Message);
        }

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
