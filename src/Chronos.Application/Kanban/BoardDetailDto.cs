namespace Chronos.Application.Kanban;

public record BoardDetailDto(Guid Id, Guid OrganizationId, string Name, Guid? ProjectId, IReadOnlyList<KanbanColumnDto> Columns);
