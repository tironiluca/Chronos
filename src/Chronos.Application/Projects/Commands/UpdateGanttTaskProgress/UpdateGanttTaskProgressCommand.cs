using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.UpdateGanttTaskProgress;

public record UpdateGanttTaskProgressCommand(Guid CallerOrganizationId, Guid ProjectId, Guid TaskId, int ProgressPercent) : IRequest<Result>;
