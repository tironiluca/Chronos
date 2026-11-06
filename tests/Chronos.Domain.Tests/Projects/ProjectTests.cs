using Chronos.Domain.Projects;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Projects;

public class ProjectTests
{
    [Fact]
    public void AddTask_WithValidDates_AddsTaskToProject()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));

        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        project.Tasks.Should().ContainSingle().Which.Should().BeSameAs(task);
    }

    [Fact]
    public void AddTask_WithEndDateBeforeStartDate_Throws()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));

        var act = () => project.AddTask("Install PLC", new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 5));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_WhenPlanned_SetsStatusToActive()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));

        project.Activate();

        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        project.Activate();

        var act = project.Activate;

        act.Should().Throw<InvalidOperationException>();
    }

    private static (Project Project, GanttTask A, GanttTask B, GanttTask C) ProjectWithThreeTasks()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var a = project.AddTask("A", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6));
        var b = project.AddTask("B", new DateOnly(2026, 1, 7), new DateOnly(2026, 1, 8));
        var c = project.AddTask("C", new DateOnly(2026, 1, 9), new DateOnly(2026, 1, 10));
        return (project, a, b, c);
    }

    [Fact]
    public void AddTaskDependency_BetweenTasksOfTheProject_AddsEdgeToSuccessor()
    {
        var (project, a, b, _) = ProjectWithThreeTasks();

        var dependency = project.AddTaskDependency(b.Id, a.Id, DependencyType.FinishToStart, 2);

        b.Dependencies.Should().ContainSingle().Which.Should().BeSameAs(dependency);
        dependency.PredecessorTaskId.Should().Be(a.Id);
        dependency.LagDays.Should().Be(2);
        a.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void AddTaskDependency_WithPredecessorOutsideTheProject_Throws()
    {
        var (project, _, b, _) = ProjectWithThreeTasks();

        var act = () => project.AddTaskDependency(b.Id, Guid.NewGuid(), DependencyType.FinishToStart);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Predecessor*not found*");
    }

    [Fact]
    public void AddTaskDependency_ForUnknownTask_Throws()
    {
        var (project, a, _, _) = ProjectWithThreeTasks();

        var act = () => project.AddTaskDependency(Guid.NewGuid(), a.Id, DependencyType.FinishToStart);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void AddTaskDependency_ClosingADirectCycle_Throws()
    {
        var (project, a, b, _) = ProjectWithThreeTasks();
        project.AddTaskDependency(b.Id, a.Id, DependencyType.FinishToStart);

        var act = () => project.AddTaskDependency(a.Id, b.Id, DependencyType.FinishToStart);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cycle*");
        a.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void AddTaskDependency_ClosingATransitiveCycle_Throws()
    {
        var (project, a, b, c) = ProjectWithThreeTasks();
        project.AddTaskDependency(b.Id, a.Id, DependencyType.FinishToStart); // A -> B
        project.AddTaskDependency(c.Id, b.Id, DependencyType.FinishToStart); // B -> C

        var act = () => project.AddTaskDependency(a.Id, c.Id, DependencyType.FinishToStart); // C -> A

        act.Should().Throw<InvalidOperationException>().WithMessage("*cycle*");
    }

    [Fact]
    public void AddTaskDependency_DiamondWithoutCycle_IsAllowed()
    {
        var (project, a, b, c) = ProjectWithThreeTasks();
        project.AddTaskDependency(b.Id, a.Id, DependencyType.FinishToStart); // A -> B
        project.AddTaskDependency(c.Id, b.Id, DependencyType.FinishToStart); // B -> C

        var act = () => project.AddTaskDependency(c.Id, a.Id, DependencyType.FinishToStart); // A -> C

        act.Should().NotThrow();
        c.Dependencies.Should().HaveCount(2);
    }

    [Fact]
    public void AddTaskDependency_OnItself_Throws()
    {
        var (project, a, _, _) = ProjectWithThreeTasks();

        var act = () => project.AddTaskDependency(a.Id, a.Id, DependencyType.FinishToStart);

        act.Should().Throw<InvalidOperationException>().WithMessage("*itself*");
    }
}
