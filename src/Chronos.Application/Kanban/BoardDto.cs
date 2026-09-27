namespace Chronos.Application.Kanban;

public record BoardDto(Guid Id, Guid OrganizationId, string Name, Guid? ProjectId);
