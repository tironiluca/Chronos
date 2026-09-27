using Chronos.Domain.Common;

namespace Chronos.Domain.Users;

public class User : AggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    // Nullable: a user isn't required to belong to a department (registration doesn't ask for
    // one yet), but at most one -- see the resource-availability epic in IMPLEMENTATION_PLAN.md.
    public Guid? DepartmentId { get; private set; }

    private User() { } // EF Core

    // passwordHash is expected to already be hashed (Application calls IPasswordHasher before
    // constructing this) -- the domain has no opinion on hashing algorithms, that's an
    // infrastructure concern (DIP).
    public User(Guid organizationId, string email, string displayName, string passwordHash, UserRole role = UserRole.Employee)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        OrganizationId = organizationId;
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    // null unassigns the user from any department.
    public void AssignDepartment(Guid? departmentId)
    {
        DepartmentId = departmentId;
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }
}
