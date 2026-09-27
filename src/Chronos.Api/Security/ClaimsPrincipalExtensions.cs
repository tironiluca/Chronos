using System.Security.Claims;
using Chronos.Domain.Users;
using Chronos.Infrastructure.Security;

namespace Chronos.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim."));

    public static Guid GetOrganizationId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ChronosClaimTypes.OrganizationId)
            ?? throw new InvalidOperationException("Missing organization claim."));

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(nameof(UserRole.Admin));
}
