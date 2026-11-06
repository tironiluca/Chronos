using Chronos.Domain.Projects;

namespace Chronos.Application.Projects;

// Project with its tasks inlined -- shared by GET /api/projects (list, consumed by the
// project x ISO-week load grid, which needs every task's date range) and GET /api/projects/{id}
// (consumed by the Gantt page). Org project/task counts are small, so the list doesn't need a
// separate tasks-less summary shape.
public record ProjectDto(
    Guid Id,
    string Name,
    string Code,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<GanttTaskDto> Tasks)
{
    public static ProjectDto From(Project project) => new(
        project.Id,
        project.Name,
        project.Code,
        project.Status,
        project.StartDate,
        project.EndDate,
        project.Tasks.Select(GanttTaskDto.From).ToList());
}
