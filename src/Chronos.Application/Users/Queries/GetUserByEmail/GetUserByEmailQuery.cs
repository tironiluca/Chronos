using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Queries.GetUserByEmail;

public record GetUserByEmailQuery(Guid CallerOrganizationId, string Email) : IRequest<Result<UserDto>>;
