namespace Chronos.Application.Kanban;

public record KanbanColumnDto(Guid Id, Guid BoardId, string Name, int Order, IReadOnlyList<KanbanCardDto> Cards);
