using Chronos.Domain.Common;

namespace Chronos.Domain.Kanban;

// A Kanban board for tracking work. ProjectId is optional -- a department board doesn't need to
// be linked to a Gantt project (see IMPLEMENTATION_PLAN.md's resource-availability epic).
public class Board : AggregateRoot
{
    private readonly List<KanbanColumn> _columns = new();

    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ProjectId { get; private set; }

    public IReadOnlyCollection<KanbanColumn> Columns => _columns.AsReadOnly();

    private Board() { } // EF Core

    public Board(Guid organizationId, string name, Guid? projectId = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name is required.", nameof(name));

        OrganizationId = organizationId;
        Name = name;
        ProjectId = projectId;
    }

    public KanbanColumn AddColumn(string name, int order)
    {
        var column = new KanbanColumn(Id, name, order);
        _columns.Add(column);
        return column;
    }
}
