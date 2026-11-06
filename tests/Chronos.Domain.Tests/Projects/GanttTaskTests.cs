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

    [Fact]
    public void AddDependency_OnSamePredecessorTwice_Throws()
    {
        var task = CreateTask();
        var predecessorId = Guid.NewGuid();
        task.AddDependency(predecessorId, DependencyType.FinishToStart);

        var act = () => task.AddDependency(predecessorId, DependencyType.StartToStart);

        act.Should().Throw<InvalidOperationException>();
        task.Dependencies.Should().ContainSingle();
    }

    [Fact]
    public void RemoveDependency_RemovesEdgeForThatPredecessor()
    {
        var task = CreateTask();
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        task.AddDependency(keep, DependencyType.FinishToStart);
        task.AddDependency(drop, DependencyType.FinishToStart);

        task.RemoveDependency(drop);

        task.Dependencies.Select(d => d.PredecessorTaskId).Should().Equal(keep);
    }

    [Fact]
    public void RemoveDependency_ForUnknownPredecessor_Throws()
    {
        var task = CreateTask();

        var act = () => task.RemoveDependency(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
