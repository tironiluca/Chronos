using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.RemoveGanttTaskDependency;

public record RemoveGanttTaskDependencyCommand(Guid CallerOrganizationId, Guid ProjectId, Guid TaskId, Guid PredecessorTaskId) : IRequest<Result>;
