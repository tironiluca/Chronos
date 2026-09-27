using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Commands.PromoteUserRole;

public class PromoteUserRoleCommandHandler : IRequestHandler<PromoteUserRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public PromoteUserRoleCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(PromoteUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        // Same failure for "not found" and "belongs to another organization" -- an Admin must
        // never be able to tell, from the response, whether a user id exists in someone else's org.
        if (user is null || user.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"User '{request.UserId}' was not found.");

        user.ChangeRole(request.NewRole);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
