using Chronos.Domain.Kanban;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Kanban;

public class BoardTests
{
    [Fact]
    public void Constructor_DefaultsProjectIdToNull()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");

        board.ProjectId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyOrganizationId_Throws()
    {
        var act = () => new Board(Guid.Empty, "Line 3 Kanban");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        var act = () => new Board(Guid.NewGuid(), "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddColumn_AddsColumnToBoard()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");

        var column = board.AddColumn("To Do", 0);

        board.Columns.Should().ContainSingle().Which.Should().BeSameAs(column);
    }

    [Fact]
    public void MoveCard_MovesCardToTargetColumnAndUpdatesColumnId()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var todo = board.AddColumn("To Do", 0);
        var done = board.AddColumn("Done", 1);
        var card = todo.AddCard("Install PLC");

        var moved = board.MoveCard(card.Id, todo.Id, done.Id);

        moved.Should().BeSameAs(card);
        todo.Cards.Should().BeEmpty();
        done.Cards.Should().ContainSingle().Which.Should().BeSameAs(card);
        card.ColumnId.Should().Be(done.Id);
    }

    [Fact]
    public void MoveCard_ToItsCurrentColumn_IsANoOp()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var todo = board.AddColumn("To Do", 0);
        var card = todo.AddCard("Install PLC");

        board.MoveCard(card.Id, todo.Id, todo.Id);

        todo.Cards.Should().ContainSingle().Which.Should().BeSameAs(card);
    }

    [Fact]
    public void MoveCard_ToColumnNotOnTheBoard_ThrowsAndLeavesCardInPlace()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var todo = board.AddColumn("To Do", 0);
        var card = todo.AddCard("Install PLC");
        var foreignColumn = new Board(Guid.NewGuid(), "Other").AddColumn("Done", 0);

        var act = () => board.MoveCard(card.Id, todo.Id, foreignColumn.Id);

        act.Should().Throw<InvalidOperationException>();
        todo.Cards.Should().ContainSingle();
        card.ColumnId.Should().Be(todo.Id);
    }

    [Fact]
    public void MoveCard_WithCardNotInSourceColumn_Throws()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var todo = board.AddColumn("To Do", 0);
        var done = board.AddColumn("Done", 1);
        var card = done.AddCard("Install PLC");

        var act = () => board.MoveCard(card.Id, todo.Id, done.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Card*not found*");
    }
}
