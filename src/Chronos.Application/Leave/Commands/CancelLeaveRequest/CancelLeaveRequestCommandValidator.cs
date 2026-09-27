using FluentValidation;

namespace Chronos.Application.Leave.Commands.CancelLeaveRequest;

public class CancelLeaveRequestCommandValidator : AbstractValidator<CancelLeaveRequestCommand>
{
    public CancelLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.CallerId).NotEmpty();
    }
}
