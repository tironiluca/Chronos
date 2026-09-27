using Chronos.Domain.Common;

namespace Chronos.Domain.Organizations;

// A grouping of users within an Organization, e.g. "Engineering" or, nested one level deeper,
// "Engineering > Platform". ParentDepartmentId is nullable and self-referencing so a department
// can have sub-departments/teams -- see IMPLEMENTATION_PLAN.md's resource-availability epic, which
// treats "department Y" as inclusive of its descendants by default.
public class Department : Entity
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public Guid? ParentDepartmentId { get; private set; }

    private Department() { } // EF Core

    public Department(Guid organizationId, string name, string code, Guid? parentDepartmentId = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Department code is required.", nameof(code));

        OrganizationId = organizationId;
        Name = name;
        Code = code.ToUpperInvariant();
        ParentDepartmentId = parentDepartmentId;
    }
}
