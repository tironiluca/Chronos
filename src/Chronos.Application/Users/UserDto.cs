namespace Chronos.Application.Users;

public record UserDto(Guid Id, string Email, string DisplayName, string Role, Guid? DepartmentId);
