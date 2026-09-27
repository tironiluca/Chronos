using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Kanban.Commands.AssignKanbanCardUser;
using Chronos.Application.Kanban.Commands.CreateBoard;
using Chronos.Application.Kanban.Commands.CreateKanbanCard;
using Chronos.Application.Kanban.Commands.CreateKanbanColumn;
using Chronos.Application.Kanban.Queries.GetBoardById;
using Chronos.Application.Kanban.Queries.GetBoardsByOrganization;

namespace Chronos.Api.Endpoints;

public static class KanbanEndpoints
{
    public static void MapKanbanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Kanban").RequireAuthorization();

        // No extra policy beyond authentication -- matches Project/GanttTask endpoints, which
        // also don't restrict creation/management to a specific role today.
        group.MapPost("/", async (CreateBoardBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateBoardCommand(user.GetOrganizationId(), body.Name, body.ProjectId);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/boards/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetBoardsByOrganizationQuery(user.GetOrganizationId()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/{boardId:guid}", async (Guid boardId, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetBoardByIdQuery(user.GetOrganizationId(), boardId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{boardId:guid}/columns", async (Guid boardId, CreateKanbanColumnBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateKanbanColumnCommand(user.GetOrganizationId(), boardId, body.Name, body.Order);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/boards/{boardId}/columns/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/{boardId:guid}/columns/{columnId:guid}/cards", async (
            Guid boardId, Guid columnId, CreateKanbanCardBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateKanbanCardCommand(user.GetOrganizationId(), boardId, columnId, body.Title, body.GanttTaskId);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/boards/{boardId}/columns/{columnId}/cards/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPatch("/{boardId:guid}/columns/{columnId:guid}/cards/{cardId:guid}/assignee", async (
            Guid boardId, Guid columnId, Guid cardId, AssignKanbanCardUserBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new AssignKanbanCardUserCommand(user.GetOrganizationId(), boardId, columnId, cardId, body.UserId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    public record CreateBoardBody(string Name, Guid? ProjectId);
    public record CreateKanbanColumnBody(string Name, int Order);
    public record CreateKanbanCardBody(string Title, Guid? GanttTaskId);
    public record AssignKanbanCardUserBody(Guid? UserId);
}
