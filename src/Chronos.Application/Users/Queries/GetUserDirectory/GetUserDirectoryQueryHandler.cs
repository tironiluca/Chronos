using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Queries.GetUserDirectory;

// Reuses IUserRepository.GetByOrganizationAsync (added for the resource-availability query) --
// no new repository method.
public class GetUserDirectoryQueryHandler : IRequestHandler<GetUserDirectoryQuery, Result<IReadOnlyList<UserDirectoryEntryDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUserDirectoryQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<UserDirectoryEntryDto>>> Handle(GetUserDirectoryQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByOrganizationAsync(request.CallerOrganizationId, cancellationToken);

        var dtos = users
            .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UserDirectoryEntryDto(u.Id, u.DisplayName, u.DepartmentId))
            .ToList();

        return Result.Success<IReadOnlyList<UserDirectoryEntryDto>>(dtos);
    }
}
