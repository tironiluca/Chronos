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
}
