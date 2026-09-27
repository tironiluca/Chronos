using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Queries.GetRightsForRole;

public class GetRightsForRoleQueryHandler : IRequestHandler<GetRightsForRoleQuery, Result<IReadOnlyList<RightDto>>>
{
    private readonly IRightRepository _rightRepository;

    public GetRightsForRoleQueryHandler(IRightRepository rightRepository)
    {
        _rightRepository = rightRepository;
    }

    public async Task<Result<IReadOnlyList<RightDto>>> Handle(GetRightsForRoleQuery request, CancellationToken cancellationToken)
    {
        var rights = await _rightRepository.GetRightsForRoleAsync(request.Role, cancellationToken);

        var dtos = rights.Select(r => new RightDto(r.Id, r.Code, r.Description)).ToList();

        return Result.Success<IReadOnlyList<RightDto>>(dtos);
    }
}
