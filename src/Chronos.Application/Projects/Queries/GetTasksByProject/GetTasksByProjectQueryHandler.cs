using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetTasksByProject;

public class GetTasksByProjectQueryHandler : IRequestHandler<GetTasksByProjectQuery, Result<IReadOnlyList<GanttTaskDto>>>
{
    private readonly IProjectRepository _projectRepository;

    public GetTasksByProjectQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<IReadOnlyList<GanttTaskDto>>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<IReadOnlyList<GanttTaskDto>>($"Project '{request.ProjectId}' was not found.");

        var dtos = project.Tasks.Select(GanttTaskDto.From).ToList();

        return Result.Success<IReadOnlyList<GanttTaskDto>>(dtos);
    }
}
