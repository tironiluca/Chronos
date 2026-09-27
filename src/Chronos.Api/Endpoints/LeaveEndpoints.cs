using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Leave.Commands.ApproveLeaveRequest;
using Chronos.Application.Leave.Commands.CancelLeaveRequest;
using Chronos.Application.Leave.Commands.CreateLeaveRequest;
using Chronos.Application.Leave.Commands.RejectLeaveRequest;
using Chronos.Application.Leave.Queries.GetLeaveRequestsByOrganization;
using Chronos.Domain.Leave;

namespace Chronos.Api.Endpoints;

public static class LeaveEndpoints
{
    public static void MapLeaveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leave-requests").WithTags("Leave").RequireAuthorization();

        // Always the caller's own organization -- an authenticated user only ever sees their
        // org's requests. Cross-org visibility for Admins is a documented follow-up, not done yet.
        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetLeaveRequestsByOrganizationQuery(user.GetOrganizationId()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (CreateLeaveRequestBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateLeaveRequestCommand(
                user.GetOrganizationId(), user.GetUserId(), body.Type, body.StartDate, body.EndDate);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/leave-requests/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new ApproveLeaveRequestCommand(id, user.GetUserId()));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization("ApproverOrAdmin");

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectLeaveRequestBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new RejectLeaveRequestCommand(id, user.GetUserId(), body.Reason));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization("ApproverOrAdmin");

        group.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new CancelLeaveRequestCommand(id, user.GetUserId(), user.IsAdmin()));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    public record CreateLeaveRequestBody(LeaveType Type, DateOnly StartDate, DateOnly EndDate);
    public record RejectLeaveRequestBody(string Reason);
}
