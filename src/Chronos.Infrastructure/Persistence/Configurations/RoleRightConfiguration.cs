using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class RoleRightConfiguration : IEntityTypeConfiguration<RoleRight>
{
    public void Configure(EntityTypeBuilder<RoleRight> builder)
    {
        builder.ToTable("RoleRights");
        builder.HasKey(rr => rr.Id);
        builder.HasIndex(rr => new { rr.Role, rr.RightId }).IsUnique();

        builder.HasData(
            Seed("21111111-1111-1111-1111-111111111101", UserRole.Employee, RightConfiguration.ViewOwnLeaveId),
            Seed("21111111-1111-1111-1111-111111111102", UserRole.Employee, RightConfiguration.RequestLeaveId),
            Seed("21111111-1111-1111-1111-111111111103", UserRole.Approver, RightConfiguration.ViewOwnLeaveId),
            Seed("21111111-1111-1111-1111-111111111104", UserRole.Approver, RightConfiguration.RequestLeaveId),
            Seed("21111111-1111-1111-1111-111111111105", UserRole.Approver, RightConfiguration.ApproveLeaveId),
            Seed("21111111-1111-1111-1111-111111111106", UserRole.Admin, RightConfiguration.ViewOwnLeaveId),
            Seed("21111111-1111-1111-1111-111111111107", UserRole.Admin, RightConfiguration.RequestLeaveId),
            Seed("21111111-1111-1111-1111-111111111108", UserRole.Admin, RightConfiguration.ApproveLeaveId),
            Seed("21111111-1111-1111-1111-111111111109", UserRole.Admin, RightConfiguration.ManageProjectsId),
            Seed("21111111-1111-1111-1111-111111111110", UserRole.Admin, RightConfiguration.ManageUsersId));
    }

    private static object Seed(string id, UserRole role, Guid rightId) =>
        new { Id = new Guid(id), Role = role, RightId = rightId };
}
