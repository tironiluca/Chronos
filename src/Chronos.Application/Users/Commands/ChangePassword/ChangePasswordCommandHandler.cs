using Chronos.Application.Common;
using Chronos.Domain.Users;
using MediatR;

namespace Chronos.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    // How many previous passwords (including the one being replaced) a user may not reuse.
    private const int RotationHistoryCount = 5;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository, IPasswordHistoryRepository passwordHistoryRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHistoryRepository = passwordHistoryRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Checked here, not just in ChangePasswordCommandValidator -- FluentValidation
        // validators are registered in DI but nothing in this codebase wires them into a
        // MediatR pipeline behavior, so validators never actually run against any command.
        if (request.NewPassword.Length < 8)
            return Result.Failure("Password must be at least 8 characters.");
        if (request.NewPassword == request.CurrentPassword)
            return Result.Failure("New password must be different from the current password.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            return Result.Failure("Current password is incorrect.");

        var recentHashes = await _passwordHistoryRepository.GetRecentAsync(request.UserId, RotationHistoryCount, cancellationToken);
        if (recentHashes.Any(h => _passwordHasher.Verify(h.PasswordHash, request.NewPassword)))
            return Result.Failure($"New password must not match any of the last {RotationHistoryCount} passwords used.");

        // Record the hash being replaced before overwriting it, so it counts toward the reuse
        // check above on the next rotation.
        await _passwordHistoryRepository.AddAsync(new PasswordHistory(user.Id, user.PasswordHash), cancellationToken);

        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));

        await _passwordHistoryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
