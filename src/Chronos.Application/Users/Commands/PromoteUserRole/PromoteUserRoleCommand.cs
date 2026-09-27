using Chronos.Application.Common;
using Chronos.Domain.Users;
using MediatR;

namespace Chronos.Application.Users.Commands.PromoteUserRole;

public record PromoteUserRoleCommand(Guid CallerOrganizationId, Guid UserId, UserRole NewRole) : IRequest<Result>;
