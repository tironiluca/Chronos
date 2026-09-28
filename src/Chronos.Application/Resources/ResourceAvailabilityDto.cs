namespace Chronos.Application.Resources;

public record ResourceAvailabilityDto(
    Guid UserId,
    string DisplayName,
    Guid? DepartmentId,
    IReadOnlyList<LeavePeriodDto> ApprovedLeave,
    IReadOnlyList<AssignedTaskDto> AssignedTasks,
    IReadOnlyList<AssignedCardDto> AssignedCards);
