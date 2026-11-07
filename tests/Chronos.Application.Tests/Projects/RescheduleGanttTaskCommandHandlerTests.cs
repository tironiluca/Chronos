using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.RescheduleGanttTask;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class RescheduleGanttTaskCommandHandlerTests
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
    public async Task Handle_WithValidDates_ReschedulesAndSaves()
    {
        var project = CreateProject();
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new RescheduleGanttTaskCommandHandler(_projectRepository);

        var result = await handler.Handle(new RescheduleGanttTaskCommand(
            _organizationId, project.Id, task.Id, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.StartDate.Should().Be(new DateOnly(2026, 2, 2));
        task.EndDate.Should().Be(new DateOnly(2026, 2, 6));
        await _projectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Handler-level guard even though the validator also rejects this -- the domain exception
    // must surface as a Failure, not a 500, if the handler is ever invoked without the pipeline.
    [Fact]
    public async Task Handle_WithEndBeforeStart_ReturnsFailureWithoutSaving()
    {
        var project = CreateProject();
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new RescheduleGanttTaskCommandHandler(_projectRepository);

        var result = await handler.Handle(new RescheduleGanttTaskCommand(
            _organizationId, project.Id, task.Id, new DateOnly(2026, 2, 6), new DateOnly(2026, 2, 2)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        task.StartDate.Should().Be(new DateOnly(2026, 1, 5));
        await _projectRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var project = CreateProject(Guid.NewGuid());
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var handler = new RescheduleGanttTaskCommandHandler(_projectRepository);

        var result = await handler.Handle(new RescheduleGanttTaskCommand(
            _organizationId, project.Id, task.Id, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        task.StartDate.Should().Be(new DateOnly(2026, 1, 5));
    }

    [Fact]
    public async Task Handle_WithUnknownTask_ReturnsFailure()
    {
        var project = CreateProject();
        var handler = new RescheduleGanttTaskCommandHandler(_projectRepository);

        var result = await handler.Handle(new RescheduleGanttTaskCommand(
            _organizationId, project.Id, Guid.NewGuid(), new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
