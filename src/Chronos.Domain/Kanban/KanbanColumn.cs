using Chronos.Domain.Common;

namespace Chronos.Domain.Kanban;

public class KanbanColumn : Entity
{
    private readonly List<KanbanCard> _cards = new();

    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = default!;
    public int Order { get; private set; }

    public IReadOnlyCollection<KanbanCard> Cards => _cards.AsReadOnly();

    private KanbanColumn() { } // EF Core

    public KanbanColumn(Guid boardId, string name, int order)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name is required.", nameof(name));

        BoardId = boardId;
        Name = name;
        Order = order;
    }

    public KanbanCard AddCard(string title, Guid? ganttTaskId = null)
    {
        var card = new KanbanCard(Id, title, ganttTaskId);
        _cards.Add(card);
        return card;
    }
}
