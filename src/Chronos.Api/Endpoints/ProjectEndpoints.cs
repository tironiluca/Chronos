using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Projects.Commands.AssignGanttTaskUser;
using Chronos.Application.Projects.Commands.CreateGanttTask;
using Chronos.Application.Projects.Commands.CreateProject;
using Chronos.Application.Projects.Queries.GetTasksByProject;

namespace Chronos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        group.MapPost("/", async (CreateProjectBody body, ClaimsPrincipal user, ISender sender) =>
        {
            // OrganizationId comes from the caller's token, never from the request body --
            // otherwise any authenticated user could create projects in someone else's org.
            var command = new CreateProjectCommand(user.GetOrganizationId(), body.Name, body.Code, body.StartDate);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        // No extra policy beyond authentication, same as project creation above -- this codebase
        // doesn't yet restrict project/task management to a specific role.
        group.MapPost("/{projectId:guid}/tasks", async (Guid projectId, CreateGanttTaskBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateGanttTaskCommand(
                user.GetOrganizationId(), projectId, body.Name, body.StartDate, body.EndDate, body.ParentTaskId);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/tasks/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{projectId:guid}/tasks", async (Guid projectId, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetTasksByProjectQuery(user.GetOrganizationId(), projectId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPatch("/{projectId:guid}/tasks/{taskId:guid}/assignee", async (
            Guid projectId, Guid taskId, AssignGanttTaskUserBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new AssignGanttTaskUserCommand(user.GetOrganizationId(), projectId, taskId, body.UserId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    public record CreateProjectBody(string Name, string Code, DateOnly StartDate);
    public record CreateGanttTaskBody(string Name, DateOnly StartDate, DateOnly EndDate, Guid? ParentTaskId);
    public record AssignGanttTaskUserBody(Guid? UserId);
}
