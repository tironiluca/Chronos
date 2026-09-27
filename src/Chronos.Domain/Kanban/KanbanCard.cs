using Chronos.Domain.Common;

namespace Chronos.Domain.Kanban;

public class KanbanCard : Entity
{
    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = default!;

    // Nullable: a card isn't required to have an assignee. Set via AssignUser, not the
    // constructor -- same treatment as GanttTask.AssignedUserId.
    public Guid? AssignedUserId { get; private set; }

    // Optional link back to a GanttTask -- lets a card mirror a Gantt-tracked task without
    // duplicating its schedule data. Plain scalar, not an EF relationship, matching this
    // codebase's convention of referencing other aggregates by id only (e.g. GanttTask.ParentTaskId).
    public Guid? GanttTaskId { get; private set; }

    private KanbanCard() { } // EF Core

    public KanbanCard(Guid columnId, string title, Guid? ganttTaskId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Card title is required.", nameof(title));

        ColumnId = columnId;
        Title = title;
        GanttTaskId = ganttTaskId;
    }

    // null unassigns the card.
    public void AssignUser(Guid? userId)
    {
        AssignedUserId = userId;
    }
}
