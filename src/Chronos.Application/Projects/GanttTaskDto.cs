namespace Chronos.Application.Projects;

public record GanttTaskDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int ProgressPercent,
    Guid? ParentTaskId,
    Guid? AssignedUserId);
