using Chronos.Application.Common;
using Chronos.Domain.Projects;
using MediatR;

namespace Chronos.Application.Projects.Commands.AddGanttTaskDependency;

// Edge PredecessorTaskId -> TaskId; both tasks must belong to ProjectId. Negative LagDays = lead.
public record AddGanttTaskDependencyCommand(
    Guid CallerOrganizationId,
    Guid ProjectId,
    Guid TaskId,
    Guid PredecessorTaskId,
    DependencyType Type,
    int LagDays) : IRequest<Result>;
