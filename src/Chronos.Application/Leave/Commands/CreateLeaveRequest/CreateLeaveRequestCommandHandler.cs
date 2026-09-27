using Chronos.Application.Common;
using Chronos.Domain.Leave;
using MediatR;

namespace Chronos.Application.Leave.Commands.CreateLeaveRequest;

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<Guid>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public CreateLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result<Guid>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = new LeaveRequest(
            request.OrganizationId,
            request.RequesterId,
            request.Type,
            request.StartDate,
            request.EndDate);

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(leaveRequest.Id);
    }
}
