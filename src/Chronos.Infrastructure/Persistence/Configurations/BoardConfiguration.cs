using Chronos.Domain.Kanban;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Persistence.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(b => b.OrganizationId);

        // ProjectId is a plain scalar, not an EF relationship -- same convention as
        // Department.ParentDepartmentId (see DepartmentConfiguration).
        builder.HasIndex(b => b.ProjectId);

        // KanbanColumn/KanbanCard are owned collections, same lifecycle/pattern as
        // Project.Tasks/GanttTask.Dependencies (see ProjectConfiguration), two levels deep here.
        builder.OwnsMany(b => b.Columns, column =>
        {
            column.ToTable("KanbanColumns");
            column.WithOwner().HasForeignKey(col => col.BoardId);
            column.HasKey(col => col.Id);

            column.Property(col => col.Name).HasMaxLength(200).IsRequired();

            column.OwnsMany(col => col.Cards, card =>
            {
                card.ToTable("KanbanCards");
                card.WithOwner().HasForeignKey(c => c.ColumnId);
                card.HasKey(c => c.Id);

                card.Property(c => c.Title).HasMaxLength(200).IsRequired();
            });
        });
    }
}
