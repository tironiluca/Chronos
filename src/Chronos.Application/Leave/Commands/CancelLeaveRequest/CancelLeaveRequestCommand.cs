using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.CancelLeaveRequest;

public record CancelLeaveRequestCommand(Guid LeaveRequestId, Guid CallerId, bool CallerIsAdmin) : IRequest<Result>;
