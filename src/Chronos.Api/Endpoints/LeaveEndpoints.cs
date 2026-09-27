using MediatR;
using Chronos.Application.Leave.Commands.ApproveLeaveRequest;
using Chronos.Application.Leave.Commands.CancelLeaveRequest;
using Chronos.Application.Leave.Commands.CreateLeaveRequest;
using Chronos.Application.Leave.Commands.RejectLeaveRequest;
using Chronos.Application.Leave.Queries.GetLeaveRequestsByOrganization;

namespace Chronos.Api.Endpoints;

public static class LeaveEndpoints
{
    public static void MapLeaveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leave-requests").WithTags("Leave");

        group.MapGet("/", async (Guid organizationId, ISender sender) =>
        {
            var result = await sender.Send(new GetLeaveRequestsByOrganizationQuery(organizationId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (CreateLeaveRequestCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/leave-requests/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveLeaveRequestBody body, ISender sender) =>
        {
            var result = await sender.Send(new ApproveLeaveRequestCommand(id, body.ApproverId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectLeaveRequestBody body, ISender sender) =>
        {
            var result = await sender.Send(new RejectLeaveRequestCommand(id, body.ApproverId, body.Reason));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelLeaveRequestCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    // Minimal API model-binds record bodies fine, but keeping the route param (id) out of the
    // JSON body needs a small wrapper type rather than binding straight to the command record.
    public record ApproveLeaveRequestBody(Guid ApproverId);
    public record RejectLeaveRequestBody(Guid ApproverId, string Reason);
}
