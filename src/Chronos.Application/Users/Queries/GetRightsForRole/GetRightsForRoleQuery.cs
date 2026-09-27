using Chronos.Application.Common;
using Chronos.Domain.Users;
using MediatR;

namespace Chronos.Application.Users.Queries.GetRightsForRole;

public record GetRightsForRoleQuery(UserRole Role) : IRequest<Result<IReadOnlyList<RightDto>>>;
