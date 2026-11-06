using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.RemoveGanttTaskDependency;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class RemoveGanttTaskDependencyCommandHandlerTests
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
    public async Task Handle_WithExistingDependency_RemovesItAndSaves()
    {
        var project = CreateProject();
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        project.AddTaskDependency(build.Id, design.Id, DependencyType.FinishToStart);
        var handler = new RemoveGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new RemoveGanttTaskDependencyCommand(_organizationId, project.Id, build.Id, design.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        build.Dependencies.Should().BeEmpty();
        await _projectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutSuchDependency_ReturnsFailure()
    {
        var project = CreateProject();
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        var handler = new RemoveGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new RemoveGanttTaskDependencyCommand(_organizationId, project.Id, build.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _projectRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailureWithoutRemoving()
    {
        var project = CreateProject(Guid.NewGuid());
        var design = project.AddTask("Design", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var build = project.AddTask("Build", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 20));
        project.AddTaskDependency(build.Id, design.Id, DependencyType.FinishToStart);
        var handler = new RemoveGanttTaskDependencyCommandHandler(_projectRepository);

        var result = await handler.Handle(new RemoveGanttTaskDependencyCommand(_organizationId, project.Id, build.Id, design.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        build.Dependencies.Should().ContainSingle();
    }
}
