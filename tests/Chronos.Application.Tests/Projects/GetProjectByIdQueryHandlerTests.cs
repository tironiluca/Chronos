using Chronos.Application.Projects;
using Chronos.Application.Projects.Queries.GetProjectById;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class GetProjectByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithProjectInCallerOrganization_ReturnsProjectWithTasks()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new GetProjectByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProjectByIdQuery(organizationId, project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("L3U");
        result.Value.Status.Should().Be(ProjectStatus.Planned);
        result.Value.Tasks.Should().ContainSingle(t => t.Id == task.Id && t.Dependencies.Count == 0);
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsSameFailureAsMissing()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new GetProjectByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProjectByIdQuery(Guid.NewGuid(), project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Project '{project.Id}' was not found.");
    }

    [Fact]
    public async Task Handle_WithUnknownProject_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Project?)null);
        var handler = new GetProjectByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProjectByIdQuery(Guid.NewGuid(), missingId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Project '{missingId}' was not found.");
    }
}
