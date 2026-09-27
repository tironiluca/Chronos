using Chronos.Domain.Projects;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Projects;

public class GanttTaskTests
{
    private static GanttTask CreateTask() =>
        new(Guid.NewGuid(), "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

    [Fact]
    public void Constructor_DefaultsAssignedUserIdToNull()
    {
        var task = CreateTask();

        task.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public void AssignUser_SetsAssignedUserId()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();

        task.AssignUser(userId);

        task.AssignedUserId.Should().Be(userId);
    }

    [Fact]
    public void AssignUser_WithNull_UnassignsTask()
    {
        var task = CreateTask();
        task.AssignUser(Guid.NewGuid());

        task.AssignUser(null);

        task.AssignedUserId.Should().BeNull();
    }
}
