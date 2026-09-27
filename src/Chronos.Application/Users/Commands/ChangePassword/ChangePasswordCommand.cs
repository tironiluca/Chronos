using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;
