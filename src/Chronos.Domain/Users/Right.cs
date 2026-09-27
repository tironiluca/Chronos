using Chronos.Domain.Common;

namespace Chronos.Domain.Users;

// A granular permission a role can be granted (e.g. "leave.approve"), independent of the
// coarse UserRole enum on User -- roles are assigned to users, rights are assigned to roles,
// so authorization checks can move to right-based policies without a schema change on User.
public class Right : Entity
{
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private Right() { } // EF Core

    public Right(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Code = code;
        Description = description;
    }
}
