using Chronos.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => new { p.OrganizationId, p.Code }).IsUnique();

        // GanttTask is modelled as an owned collection: same aggregate lifecycle as Project,
        // but persisted in its own table so it can still be queried and indexed directly.
        builder.OwnsMany(p => p.Tasks, task =>
        {
            task.ToTable("GanttTasks");
            task.WithOwner().HasForeignKey(t => t.ProjectId);
            task.HasKey(t => t.Id);

            task.Property(t => t.Name).HasMaxLength(200).IsRequired();

            task.OwnsMany(t => t.Dependencies, dep =>
            {
                dep.ToTable("GanttTaskDependencies");
                dep.WithOwner().HasForeignKey("GanttTaskId");
                dep.Property<Guid>("Id");
                dep.HasKey("Id");
            });
        });
    }
}
