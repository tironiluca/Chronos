using FluentValidation;

namespace Chronos.Application.Leave.Commands.ApproveLeaveRequest;

public class ApproveLeaveRequestCommandValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.ApproverId).NotEmpty();
    }
}
