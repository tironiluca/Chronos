namespace Chronos.Application.Users;

public record LoginResultDto(
    Guid UserId,
    Guid OrganizationId,
    string Email,
    string DisplayName,
    string Role,
    string Token,
    DateTime ExpiresAtUtc);
