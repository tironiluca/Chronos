using Chronos.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();

        // Unique per organization, not globally -- two different companies may both use "ENG".
        builder.HasIndex(d => new { d.OrganizationId, d.Code }).IsUnique();

        // ParentDepartmentId is a plain scalar, not an EF relationship -- matches this codebase's
        // convention of referencing other aggregates/records by id only (see e.g. User.OrganizationId,
        // RoleRight.RightId), so cross-tenant/tree lookups stay explicit in repositories rather than
        // relying on EF-managed FK constraints or cascades.
        builder.HasIndex(d => d.ParentDepartmentId);
    }
}
