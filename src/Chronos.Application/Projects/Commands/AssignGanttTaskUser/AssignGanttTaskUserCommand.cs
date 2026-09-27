using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.AssignGanttTaskUser;

// UserId null unassigns the task.
public record AssignGanttTaskUserCommand(Guid CallerOrganizationId, Guid ProjectId, Guid TaskId, Guid? UserId) : IRequest<Result>;
