using Chronos.Application.Projects;
using Chronos.Application.Projects.Queries.GetProjectsByOrganization;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class GetProjectsByOrganizationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsProjectsOrderedByName_WithTasksInlined()
    {
        var organizationId = Guid.NewGuid();
        var zeta = new Project(organizationId, "Zeta Rollout", "ZR", new DateOnly(2026, 1, 1));
        var alpha = new Project(organizationId, "Alpha Upgrade", "AU", new DateOnly(2026, 2, 1));
        var predecessor = alpha.AddTask("Design", new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 6));
        var successor = alpha.AddTask("Build", new DateOnly(2026, 2, 9), new DateOnly(2026, 2, 20));
        successor.AddDependency(predecessor.Id, DependencyType.FinishToStart);
        var repository = Substitute.For<IProjectRepository>();
        repository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new List<Project> { zeta, alpha });
        var handler = new GetProjectsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new GetProjectsByOrganizationQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(p => p.Name).Should().Equal("Alpha Upgrade", "Zeta Rollout");
        var alphaDto = result.Value![0];
        alphaDto.Tasks.Should().HaveCount(2);
        alphaDto.Tasks.Single(t => t.Id == successor.Id).Dependencies.Should().Equal(predecessor.Id);
        result.Value![1].Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoProjects_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var repository = Substitute.For<IProjectRepository>();
        repository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new List<Project>());
        var handler = new GetProjectsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new GetProjectsByOrganizationQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
