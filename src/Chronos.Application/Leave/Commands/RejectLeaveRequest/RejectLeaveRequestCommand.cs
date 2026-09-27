using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.RejectLeaveRequest;

public record RejectLeaveRequestCommand(Guid LeaveRequestId, Guid ApproverId, string Reason) : IRequest<Result>;
