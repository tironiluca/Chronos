using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        // Cross-org = same not-found error as a missing id, so existence isn't leaked.
        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<ProjectDto>($"Project '{request.ProjectId}' was not found.");

        return Result.Success(ProjectDto.From(project));
    }
}
