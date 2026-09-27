using Chronos.Domain.Common;

namespace Chronos.Domain.Users;

// Join row: which Rights a UserRole grants. Keyed on the existing UserRole enum rather than a
// separate Role table, since UserRole is already the source of truth for User.Role and
// PromoteUserRoleCommand -- this is additive, not a replacement.
public class RoleRight : Entity
{
    public UserRole Role { get; private set; }
    public Guid RightId { get; private set; }

    private RoleRight() { } // EF Core

    public RoleRight(UserRole role, Guid rightId)
    {
        Role = role;
        RightId = rightId;
    }
}
