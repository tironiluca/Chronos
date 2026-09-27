using Chronos.Domain.Common;

namespace Chronos.Domain.Users;

// One row per password a user has ever had (including the current one, recorded at the moment
// it's superseded), so ChangePasswordCommandHandler can block reuse of recent passwords.
public class PasswordHistory : Entity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private PasswordHistory() { } // EF Core

    public PasswordHistory(Guid userId, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
