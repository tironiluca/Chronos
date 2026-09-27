using Chronos.Application.Leave;
using Chronos.Application.Leave.Commands.ApproveLeaveRequest;
using Chronos.Application.Leave.Commands.CancelLeaveRequest;
using Chronos.Application.Leave.Commands.CreateLeaveRequest;
using Chronos.Application.Leave.Commands.RejectLeaveRequest;
using Chronos.Application.Leave.Queries.GetLeaveRequestsByOrganization;
using Chronos.Domain.Leave;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Leave;

public class LeaveRequestHandlersTests
{
    [Fact]
    public async Task CreateLeaveRequest_WithValidCommand_PersistsAndReturnsItsId()
    {
        var repository = Substitute.For<ILeaveRequestRepository>();
        var handler = new CreateLeaveRequestCommandHandler(repository);
        var command = new CreateLeaveRequestCommand(
            Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await repository.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenRequestDoesNotExist_ReturnsFailure()
    {
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new ApproveLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(new ApproveLeaveRequestCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("was not found");
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenPending_ApprovesAndSaves()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new ApproveLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(new ApproveLeaveRequestCommand(leaveRequest.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Approved);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenAlreadyApproved_ReturnsFailureInsteadOfThrowing()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        leaveRequest.Approve(Guid.NewGuid());
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new ApproveLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(new ApproveLeaveRequestCommand(leaveRequest.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public async Task RejectLeaveRequest_WhenPending_RejectsWithReason()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new RejectLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(leaveRequest.Id, Guid.NewGuid(), "Understaffed that week"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Rejected);
        leaveRequest.RejectionReason.Should().Be("Understaffed that week");
    }

    [Fact]
    public async Task CancelLeaveRequest_WhenPending_Cancels()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new CancelLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(
            new CancelLeaveRequestCommand(leaveRequest.Id, leaveRequest.RequesterId, CallerIsAdmin: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Cancelled);
    }

    [Fact]
    public async Task CancelLeaveRequest_WhenCallerIsNotRequesterOrAdmin_ReturnsFailure()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new CancelLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(
            new CancelLeaveRequestCommand(leaveRequest.Id, Guid.NewGuid(), CallerIsAdmin: false), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        leaveRequest.Status.Should().Be(LeaveStatus.Pending);
    }

    [Fact]
    public async Task CancelLeaveRequest_WhenCallerIsAdmin_CancelsEvenIfNotRequester()
    {
        var leaveRequest = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var handler = new CancelLeaveRequestCommandHandler(repository);

        var result = await handler.Handle(
            new CancelLeaveRequestCommand(leaveRequest.Id, Guid.NewGuid(), CallerIsAdmin: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Cancelled);
    }

    [Fact]
    public async Task GetLeaveRequestsByOrganization_MapsDomainEntitiesToDtos()
    {
        var organizationId = Guid.NewGuid();
        var leaveRequest = new LeaveRequest(organizationId, Guid.NewGuid(), LeaveType.SickLeave,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3));
        var repository = Substitute.For<ILeaveRequestRepository>();
        repository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { leaveRequest });
        var handler = new GetLeaveRequestsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new GetLeaveRequestsByOrganizationQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Type == "SickLeave" && dto.Status == "Pending");
    }
}
