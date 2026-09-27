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
}
