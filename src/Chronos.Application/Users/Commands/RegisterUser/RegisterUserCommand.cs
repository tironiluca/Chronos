using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(Guid OrganizationId, string Email, string DisplayName, string Password) : IRequest<Result<Guid>>;
