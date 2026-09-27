using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PasswordHash).IsRequired();
        builder.HasIndex(h => new { h.UserId, h.CreatedAtUtc });
    }
}
