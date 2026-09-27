using Chronos.Application.Common;
using Chronos.Domain.Users;
using MediatR;

namespace Chronos.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>($"Email '{request.Email}' is already registered.");

        var passwordHash = _passwordHasher.Hash(request.Password);

        // Self-registration always starts as Employee. Promoting to Approver/Admin is not
        // implemented yet -- it needs to be an admin-only operation, not something the
        // registering client can request for itself. See README.
        var user = new User(request.OrganizationId, request.Email, request.DisplayName, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
