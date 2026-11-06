namespace Chronos.Application.Users;

// Deliberately narrower than UserDto: no Email/Role. The directory is readable by every org member
// (to populate assignee pickers), whereas email/role lookup stays AdminOnly.
public record UserDirectoryEntryDto(Guid Id, string DisplayName, Guid? DepartmentId);
