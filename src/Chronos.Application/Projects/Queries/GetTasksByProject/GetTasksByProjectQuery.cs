using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetTasksByProject;

public record GetTasksByProjectQuery(Guid CallerOrganizationId, Guid ProjectId) : IRequest<Result<IReadOnlyList<GanttTaskDto>>>;
