using Chronos.Domain.Common;

namespace Chronos.Domain.Organizations;

// A single company within the intra-group (e.g. Sealed Air Rho, Sealed Air Simpsonville).
public class Organization : Entity
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!; // short code used in cross-org reporting

    private Organization() { } // EF Core

    public Organization(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Organization code is required.", nameof(code));

        Name = name;
        Code = code.ToUpperInvariant();
    }
}
