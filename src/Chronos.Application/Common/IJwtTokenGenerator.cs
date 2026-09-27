using Chronos.Domain.Users;

namespace Chronos.Application.Common;

public interface IJwtTokenGenerator
{
    AuthTokenDto Generate(User user);
}
