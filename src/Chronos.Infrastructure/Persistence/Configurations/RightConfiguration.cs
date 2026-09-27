using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class RightConfiguration : IEntityTypeConfiguration<Right>
{
    // Fixed ids so seed data is stable across migrations/environments -- never regenerate these.
    public static readonly Guid ViewOwnLeaveId = new("11111111-1111-1111-1111-111111111101");
    public static readonly Guid RequestLeaveId = new("11111111-1111-1111-1111-111111111102");
    public static readonly Guid ApproveLeaveId = new("11111111-1111-1111-1111-111111111103");
    public static readonly Guid ManageProjectsId = new("11111111-1111-1111-1111-111111111104");
    public static readonly Guid ManageUsersId = new("11111111-1111-1111-1111-111111111105");

    public void Configure(EntityTypeBuilder<Right> builder)
    {
        builder.ToTable("Rights");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(400).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        // HasData takes property-name-matched anonymous objects, not real instances -- EF writes
        // them straight into the model's shadow/private-setter properties, bypassing the
        // constructor entirely.
        builder.HasData(
            new { Id = ViewOwnLeaveId, Code = "leave.view-own", Description = "View the caller's own leave requests." },
            new { Id = RequestLeaveId, Code = "leave.request", Description = "Submit a new leave request." },
            new { Id = ApproveLeaveId, Code = "leave.approve", Description = "Approve or reject a leave request." },
            new { Id = ManageProjectsId, Code = "projects.manage", Description = "Create and manage projects." },
            new { Id = ManageUsersId, Code = "users.manage", Description = "Promote/demote users within the organization." });
    }
}
