namespace Chronos.Application.Resources;

public record AssignedTaskDto(Guid Id, Guid ProjectId, string Name, DateOnly StartDate, DateOnly EndDate);
