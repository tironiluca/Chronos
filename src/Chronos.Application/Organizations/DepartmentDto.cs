namespace Chronos.Application.Organizations;

public record DepartmentDto(Guid Id, Guid OrganizationId, string Name, string Code, Guid? ParentDepartmentId);
