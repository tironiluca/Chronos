using Chronos.Application.Organizations;
using Chronos.Application.Organizations.Queries.GetDepartmentsByOrganization;
using Chronos.Domain.Organizations;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Organizations;

public class GetDepartmentsByOrganizationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDepartmentsMappedToDtos()
    {
        var organizationId = Guid.NewGuid();
        var department = new Department(organizationId, "Engineering", "ENG");
        var repository = Substitute.For<IDepartmentRepository>();
        repository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new[] { department });
        var handler = new GetDepartmentsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new GetDepartmentsByOrganizationQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == department.Id && d.Code == "ENG");
    }
}
