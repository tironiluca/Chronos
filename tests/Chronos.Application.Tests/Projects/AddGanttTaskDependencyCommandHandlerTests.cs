using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.AddGanttTaskDependency;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class AddGanttTaskDependencyCommandHandlerTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();

    private Project CreateProject(Guid? organizationId = null)
    {
        var project = new Project(organizationId ?? _organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        return project;
    }

    [Fact]
    public async Task Handle_BetweenTasksOfTheProject_AddsDependencyAndSaves()
    {
        var project = CreateProject();
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        var handler = new AddGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new AddGanttTaskDependencyCommand(
            _organizationId, project.Id, build.Id, design.Id, DependencyType.FinishToStart, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        build.Dependencies.Should().ContainSingle(d => d.PredecessorTaskId == design.Id && d.LagDays == 1);
        await _projectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosingACycle_ReturnsFailureWithoutSaving()
    {
        var project = CreateProject();
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        project.AddTaskDependency(build.Id, design.Id, DependencyType.FinishToStart);
        var handler = new AddGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new AddGanttTaskDependencyCommand(
            _organizationId, project.Id, design.Id, build.Id, DependencyType.FinishToStart, 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cycle");
        await _projectRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPredecessorFromAnotherProject_ReturnsFailure()
    {
        var project = CreateProject();
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        var foreignTask = CreateProject().AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new AddGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new AddGanttTaskDependencyCommand(
            _organizationId, project.Id, build.Id, foreignTask.Id, DependencyType.FinishToStart, 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        build.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var project = CreateProject(Guid.NewGuid());
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        var handler = new AddGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new AddGanttTaskDependencyCommand(
            _organizationId, project.Id, build.Id, design.Id, DependencyType.FinishToStart, 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        build.Dependencies.Should().BeEmpty();
    }
}
