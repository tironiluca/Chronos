using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.UpdateGanttTaskProgress;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class UpdateGanttTaskProgressCommandHandlerTests
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
    public async Task Handle_WithPercentInRange_UpdatesAndSaves()
    {
        var project = CreateProject();
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new UpdateGanttTaskProgressCommandHandler(_projectRepository);

        var result = await handler.Handle(new UpdateGanttTaskProgressCommand(_organizationId, project.Id, task.Id, 75), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.ProgressPercent.Should().Be(75);
        await _projectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPercentOutOfRange_ReturnsFailureWithoutSaving()
    {
        var project = CreateProject();
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new UpdateGanttTaskProgressCommandHandler(_projectRepository);

        var result = await handler.Handle(new UpdateGanttTaskProgressCommand(_organizationId, project.Id, task.Id, 120), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        task.ProgressPercent.Should().Be(0);
        await _projectRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var project = CreateProject(Guid.NewGuid());
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new UpdateGanttTaskProgressCommandHandler(_projectRepository);

        var result = await handler.Handle(new UpdateGanttTaskProgressCommand(_organizationId, project.Id, task.Id, 50), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        task.ProgressPercent.Should().Be(0);
    }
}
