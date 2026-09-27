namespace Chronos.Application.Common;

public record AuthTokenDto(string Token, DateTime ExpiresAtUtc);
