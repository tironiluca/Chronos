using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.CreateGanttTask;

public record CreateGanttTaskCommand(
    Guid CallerOrganizationId,
    Guid ProjectId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ParentTaskId) : IRequest<Result<Guid>>;
