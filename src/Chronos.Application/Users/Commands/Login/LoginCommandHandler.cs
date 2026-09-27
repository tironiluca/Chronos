using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Deliberately the same error for "no such user" and "wrong password" -- do not let a
        // caller enumerate which emails are registered.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            return Result.Failure<LoginResultDto>("Invalid email or password.");

        var token = _jwtTokenGenerator.Generate(user);

        return Result.Success(new LoginResultDto(
            user.Id, user.OrganizationId, user.Email, user.DisplayName, user.Role.ToString(),
            token.Token, token.ExpiresAtUtc));
    }
}
