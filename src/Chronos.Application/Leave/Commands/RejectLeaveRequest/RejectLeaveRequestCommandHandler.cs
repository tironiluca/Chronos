using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.RejectLeaveRequest;

public class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public RejectLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure($"Leave request '{request.LeaveRequestId}' was not found.");

        try
        {
            leaveRequest.Reject(request.ApproverId, request.Reason);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Failure(ex.Message);
        }

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
