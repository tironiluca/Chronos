using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.CancelLeaveRequest;

public class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public CancelLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure($"Leave request '{request.LeaveRequestId}' was not found.");

        if (leaveRequest.RequesterId != request.CallerId && !request.CallerIsAdmin)
            return Result.Failure("Only the requester or an admin can cancel this request.");

        try
        {
            leaveRequest.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
