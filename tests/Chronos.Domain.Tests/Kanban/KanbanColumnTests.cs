using Chronos.Domain.Kanban;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Kanban;

public class KanbanColumnTests
{
    private static KanbanColumn CreateColumn() => new(Guid.NewGuid(), "To Do", 0);

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        var act = () => new KanbanColumn(Guid.NewGuid(), " ", 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddCard_AddsCardToColumn()
    {
        var column = CreateColumn();

        var card = column.AddCard("Install PLC");

        column.Cards.Should().ContainSingle().Which.Should().BeSameAs(card);
    }

    [Fact]
    public void AddCard_WithGanttTaskId_SetsGanttTaskIdOnCard()
    {
        var column = CreateColumn();
        var ganttTaskId = Guid.NewGuid();

        var card = column.AddCard("Install PLC", ganttTaskId);

        card.GanttTaskId.Should().Be(ganttTaskId);
    }
}
