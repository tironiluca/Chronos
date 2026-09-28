using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Queries.GetUserByEmail;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByEmailQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same not-found-for-both-cases shape as everywhere else -- a caller can't tell "no such
        // user" from "exists, but in someone else's organization".
        if (user is null || user.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<UserDto>($"User with email '{request.Email}' was not found.");

        var dto = new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.DepartmentId);

        return Result.Success(dto);
    }
}
