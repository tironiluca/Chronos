using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Leave.Commands.ApproveLeaveRequest;

public record ApproveLeaveRequestCommand(Guid LeaveRequestId, Guid ApproverId) : IRequest<Result>;
