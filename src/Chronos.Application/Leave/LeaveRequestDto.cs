namespace Chronos.Application.Leave;

public record LeaveRequestDto(
    Guid Id,
    Guid OrganizationId,
    Guid RequesterId,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    Guid? ApproverId,
    string? RejectionReason);
