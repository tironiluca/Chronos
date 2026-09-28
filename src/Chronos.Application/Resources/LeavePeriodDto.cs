namespace Chronos.Application.Resources;

public record LeavePeriodDto(Guid Id, DateOnly StartDate, DateOnly EndDate, string Type);
