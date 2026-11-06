using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetProjectsByOrganization;

// Reuses GetDetailedByOrganizationAsync (added for the resource-availability query) rather than a
// new repository method -- callers need each project's tasks, not just the project rows.
public class GetProjectsByOrganizationQueryHandler : IRequestHandler<GetProjectsByOrganizationQuery, Result<IReadOnlyList<ProjectDto>>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsByOrganizationQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetProjectsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetDetailedByOrganizationAsync(request.CallerOrganizationId, cancellationToken);

        var dtos = projects
            .OrderBy(p => p.Name)
            .Select(ProjectDto.From)
            .ToList();

        return Result.Success<IReadOnlyList<ProjectDto>>(dtos);
    }
}
