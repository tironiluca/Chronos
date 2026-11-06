using Chronos.Domain.Projects;

namespace Chronos.Application.Projects;

// Dependencies = predecessor task ids (TaskDependency.PredecessorTaskId), which is what the
// frontend's Gantt renderer draws arrows from.
public record GanttTaskDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int ProgressPercent,
    Guid? ParentTaskId,
    Guid? AssignedUserId,
    IReadOnlyList<Guid> Dependencies)
{
    public static GanttTaskDto From(GanttTask task) => new(
        task.Id,
        task.ProjectId,
        task.Name,
        task.StartDate,
        task.EndDate,
        task.ProgressPercent,
        task.ParentTaskId,
        task.AssignedUserId,
        task.Dependencies.Select(d => d.PredecessorTaskId).ToList());
}
