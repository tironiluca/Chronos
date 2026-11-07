using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.RescheduleGanttTask;

public record RescheduleGanttTaskCommand(Guid CallerOrganizationId, Guid ProjectId, Guid TaskId, DateOnly StartDate, DateOnly EndDate) : IRequest<Result>;
