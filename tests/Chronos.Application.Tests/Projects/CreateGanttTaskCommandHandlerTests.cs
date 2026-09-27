using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.CreateGanttTask;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class CreateGanttTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithProjectInCallerOrganization_AddsTaskAndReturnsItsId()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new CreateGanttTaskCommandHandler(repository);

        var result = await handler.Handle(
            new CreateGanttTaskCommand(organizationId, project.Id, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Tasks.Should().ContainSingle(t => t.Id == result.Value && t.Name == "Install PLC");
        // TrackNewTask must be called -- see ProjectRepository for why (EF Core would otherwise
        // mistake the new owned entity for an existing one and issue a failing UPDATE).
        repository.Received(1).TrackNewTask(Arg.Is<GanttTask>(t => t.Id == result.Value));
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailureWithoutAddingTask()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new CreateGanttTaskCommandHandler(repository);

        var result = await handler.Handle(
            new CreateGanttTaskCommand(Guid.NewGuid(), project.Id, "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        project.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownProject_ReturnsFailure()
    {
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);
        var handler = new CreateGanttTaskCommandHandler(repository);

        var result = await handler.Handle(
            new CreateGanttTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
