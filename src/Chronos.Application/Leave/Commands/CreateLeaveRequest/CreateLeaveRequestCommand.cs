using Chronos.Application.Common;
using Chronos.Domain.Leave;
using MediatR;

namespace Chronos.Application.Leave.Commands.CreateLeaveRequest;

public record CreateLeaveRequestCommand(
    Guid OrganizationId,
    Guid RequesterId,
    LeaveType Type,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<Result<Guid>>;
