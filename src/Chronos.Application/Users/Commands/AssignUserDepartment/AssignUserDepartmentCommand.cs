using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.AssignUserDepartment;

// DepartmentId null unassigns the user from any department.
public record AssignUserDepartmentCommand(Guid CallerOrganizationId, Guid UserId, Guid? DepartmentId) : IRequest<Result>;
