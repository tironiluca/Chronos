using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Resources.Queries.GetResourceAvailability;

// DepartmentId is inclusive of descendants (see IMPLEMENTATION_PLAN.md's resource-availability
// epic) -- resolved by the handler, not here. ProjectId, if given, scopes which Gantt tasks/Kanban
// boards are considered; it never scopes the LeaveRequest side, since leave isn't project-related.
public record GetResourceAvailabilityQuery(
    Guid CallerOrganizationId,
    Guid? DepartmentId,
    Guid? ProjectId,
    DateOnly From,
    DateOnly To) : IRequest<Result<IReadOnlyList<ResourceAvailabilityDto>>>;
