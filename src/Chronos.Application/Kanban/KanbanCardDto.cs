namespace Chronos.Application.Kanban;

public record KanbanCardDto(Guid Id, Guid ColumnId, string Title, Guid? AssignedUserId, Guid? GanttTaskId);
