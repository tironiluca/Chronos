using Chronos.Application.Projects;
using Chronos.Application.Projects.Queries.GetTasksByProject;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class GetTasksByProjectQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithProjectInCallerOrganization_ReturnsItsTasksAsDtos()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        task.AssignUser(Guid.NewGuid());
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new GetTasksByProjectQueryHandler(repository);

        var result = await handler.Handle(new GetTasksByProjectQuery(organizationId, project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(t => t.Id == task.Id && t.AssignedUserId == task.AssignedUserId);
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var repository = Substitute.For<IProjectRepository>();
        repository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new GetTasksByProjectQueryHandler(repository);

        var result = await handler.Handle(new GetTasksByProjectQuery(Guid.NewGuid(), project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
