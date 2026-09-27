using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResultDto>>;
