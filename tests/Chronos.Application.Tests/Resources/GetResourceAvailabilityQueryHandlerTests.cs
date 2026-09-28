using Chronos.Application.Kanban;
using Chronos.Application.Leave;
using Chronos.Application.Organizations;
using Chronos.Application.Projects;
using Chronos.Application.Resources.Queries.GetResourceAvailability;
using Chronos.Application.Users;
using Chronos.Domain.Kanban;
using Chronos.Domain.Leave;
using Chronos.Domain.Organizations;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Resources;

public class GetResourceAvailabilityQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departmentRepository = Substitute.For<IDepartmentRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IBoardRepository _boardRepository = Substitute.For<IBoardRepository>();
    private readonly ILeaveRequestRepository _leaveRequestRepository = Substitute.For<ILeaveRequestRepository>();

    private GetResourceAvailabilityQueryHandler CreateHandler() => new(
        _userRepository, _departmentRepository, _projectRepository, _boardRepository, _leaveRequestRepository);

    [Fact]
    public async Task Handle_ReturnsApprovedLeaveAssignedTasksAndCards_OverlappingTheRange()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "worker@example.com", "Worker One", "hash");

        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 10));
        task.AssignUser(user.Id);

        var board = new Board(organizationId, "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        var card = column.AddCard("Wire panel");
        card.AssignUser(user.Id);

        var leaveRequest = new LeaveRequest(organizationId, user.Id, LeaveType.Vacation, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3));
        leaveRequest.Approve(Guid.NewGuid());

        _userRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { user });
        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { project });
        _boardRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { board });
        _leaveRequestRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { leaveRequest });

        var query = new GetResourceAvailabilityQuery(organizationId, null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var availability = result.Value.Should().ContainSingle(a => a.UserId == user.Id).Subject;
        availability.ApprovedLeave.Should().ContainSingle(l => l.Id == leaveRequest.Id);
        availability.AssignedTasks.Should().ContainSingle(t => t.Id == task.Id);
        availability.AssignedCards.Should().ContainSingle(c => c.Id == card.Id);
    }

    [Fact]
    public async Task Handle_ExcludesLeaveAndTasksOutsideTheRequestedDateRange()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "worker@example.com", "Worker One", "hash");

        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5));
        task.AssignUser(user.Id);

        var leaveRequest = new LeaveRequest(organizationId, user.Id, LeaveType.Vacation, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3));
        leaveRequest.Approve(Guid.NewGuid());

        _userRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { user });
        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { project });
        _boardRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Board>());
        _leaveRequestRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { leaveRequest });

        var query = new GetResourceAvailabilityQuery(organizationId, null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var availability = result.Value.Should().ContainSingle(a => a.UserId == user.Id).Subject;
        availability.ApprovedLeave.Should().BeEmpty();
        availability.AssignedTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesPendingLeave_OnlyApprovedIsShared()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "worker@example.com", "Worker One", "hash");
        var pendingLeave = new LeaveRequest(organizationId, user.Id, LeaveType.Vacation, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3));

        _userRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { user });
        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Project>());
        _boardRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Board>());
        _leaveRequestRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { pendingLeave });

        var query = new GetResourceAvailabilityQuery(organizationId, null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.UserId == user.Id).Subject.ApprovedLeave.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithDepartmentId_IncludesUsersInDescendantDepartmentsToo()
    {
        var organizationId = Guid.NewGuid();
        var parent = new Department(organizationId, "Engineering", "ENG");
        var child = new Department(organizationId, "Platform", "PLAT", parent.Id);

        var userInParent = new User(organizationId, "parent@example.com", "Parent User", "hash");
        userInParent.AssignDepartment(parent.Id);
        var userInChild = new User(organizationId, "child@example.com", "Child User", "hash");
        userInChild.AssignDepartment(child.Id);
        var userElsewhere = new User(organizationId, "elsewhere@example.com", "Elsewhere User", "hash");

        _departmentRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { parent, child });
        _userRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new[] { userInParent, userInChild, userElsewhere });
        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Project>());
        _boardRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Board>());
        _leaveRequestRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<LeaveRequest>());

        var query = new GetResourceAvailabilityQuery(organizationId, parent.Id, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(a => a.UserId).Should().BeEquivalentTo(new[] { userInParent.Id, userInChild.Id });
    }

    [Fact]
    public async Task Handle_WithDepartmentInAnotherOrganization_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();
        var otherOrgDepartment = new Department(Guid.NewGuid(), "Other Org Dept", "OOD");

        _departmentRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Department>());

        var query = new GetResourceAvailabilityQuery(organizationId, otherOrgDepartment.Id, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithProjectId_ScopesTasksAndCardsToThatProjectOnly()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "worker@example.com", "Worker One", "hash");

        var scopedProject = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var scopedTask = scopedProject.AddTask("Install PLC", new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 10));
        scopedTask.AssignUser(user.Id);

        var otherProject = new Project(organizationId, "Line 4 Upgrade", "L4U", new DateOnly(2026, 1, 1));
        var otherTask = otherProject.AddTask("Install sensor", new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 10));
        otherTask.AssignUser(user.Id);

        var scopedBoard = new Board(organizationId, "Line 3 Kanban", scopedProject.Id);
        var scopedCard = scopedBoard.AddColumn("To Do", 0).AddCard("Wire panel");
        scopedCard.AssignUser(user.Id);

        var otherBoard = new Board(organizationId, "Standalone Kanban");
        var otherCard = otherBoard.AddColumn("To Do", 0).AddCard("Unrelated card");
        otherCard.AssignUser(user.Id);

        _userRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new[] { user });
        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new[] { scopedProject, otherProject });
        _boardRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new[] { scopedBoard, otherBoard });
        _leaveRequestRepository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<LeaveRequest>());

        var query = new GetResourceAvailabilityQuery(organizationId, null, scopedProject.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var availability = result.Value.Should().ContainSingle(a => a.UserId == user.Id).Subject;
        availability.AssignedTasks.Should().ContainSingle(t => t.Id == scopedTask.Id);
        availability.AssignedCards.Should().ContainSingle(c => c.Id == scopedCard.Id);
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();
        var otherOrgProject = new Project(Guid.NewGuid(), "Other Org Project", "OOP", new DateOnly(2026, 1, 1));

        _projectRepository.GetDetailedByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Project>());

        var query = new GetResourceAvailabilityQuery(organizationId, null, otherOrgProject.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
