using Chronos.Domain.Kanban;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Kanban;

public class KanbanCardTests
{
    private static KanbanCard CreateCard() => new(Guid.NewGuid(), "Install PLC");

    [Fact]
    public void Constructor_WithBlankTitle_Throws()
    {
        var act = () => new KanbanCard(Guid.NewGuid(), " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_DefaultsAssignedUserIdToNull()
    {
        var card = CreateCard();

        card.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public void AssignUser_SetsAssignedUserId()
    {
        var card = CreateCard();
        var userId = Guid.NewGuid();

        card.AssignUser(userId);

        card.AssignedUserId.Should().Be(userId);
    }

    [Fact]
    public void AssignUser_WithNull_UnassignsCard()
    {
        var card = CreateCard();
        card.AssignUser(Guid.NewGuid());

        card.AssignUser(null);

        card.AssignedUserId.Should().BeNull();
    }
}
