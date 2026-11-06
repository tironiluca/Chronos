using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Projects.Commands.AddGanttTaskDependency;
using Chronos.Application.Projects.Commands.AssignGanttTaskUser;
using Chronos.Application.Projects.Commands.CreateGanttTask;
using Chronos.Application.Projects.Commands.CreateProject;
using Chronos.Application.Projects.Commands.RemoveGanttTaskDependency;
using Chronos.Application.Projects.Commands.RescheduleGanttTask;
using Chronos.Application.Projects.Commands.UpdateGanttTaskProgress;
using Chronos.Application.Projects.Queries.GetProjectById;
using Chronos.Application.Projects.Queries.GetProjectsByOrganization;
using Chronos.Application.Projects.Queries.GetTasksByProject;
using Chronos.Domain.Projects;

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

        // Any authenticated org member may list/read projects -- same as GET /api/boards and
        // GET /api/departments. Each project carries its tasks inline (see ProjectDto).
        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetProjectsByOrganizationQuery(user.GetOrganizationId()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/{projectId:guid}", async (Guid projectId, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetProjectByIdQuery(user.GetOrganizationId(), projectId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
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

        // Edit endpoints -- same scoping/policy as the assignee PATCH above.
        group.MapPatch("/{projectId:guid}/tasks/{taskId:guid}/schedule", async (
            Guid projectId, Guid taskId, RescheduleGanttTaskBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new RescheduleGanttTaskCommand(
                user.GetOrganizationId(), projectId, taskId, body.StartDate, body.EndDate));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        group.MapPatch("/{projectId:guid}/tasks/{taskId:guid}/progress", async (
            Guid projectId, Guid taskId, UpdateGanttTaskProgressBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new UpdateGanttTaskProgressCommand(
                user.GetOrganizationId(), projectId, taskId, body.ProgressPercent));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        // A dependency is identified by its predecessor (at most one edge per predecessor per
        // task -- see GanttTask.AddDependency), so it needs no id of its own in the URL.
        group.MapPost("/{projectId:guid}/tasks/{taskId:guid}/dependencies", async (
            Guid projectId, Guid taskId, AddGanttTaskDependencyBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new AddGanttTaskDependencyCommand(
                user.GetOrganizationId(), projectId, taskId, body.PredecessorTaskId, body.Type, body.LagDays));
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/tasks/{taskId}/dependencies/{body.PredecessorTaskId}", body.PredecessorTaskId)
                : Results.BadRequest(result.Error);
        });

        group.MapDelete("/{projectId:guid}/tasks/{taskId:guid}/dependencies/{predecessorTaskId:guid}", async (
            Guid projectId, Guid taskId, Guid predecessorTaskId, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new RemoveGanttTaskDependencyCommand(
                user.GetOrganizationId(), projectId, taskId, predecessorTaskId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    public record CreateProjectBody(string Name, string Code, DateOnly StartDate);
    public record CreateGanttTaskBody(string Name, DateOnly StartDate, DateOnly EndDate, Guid? ParentTaskId);
    public record AssignGanttTaskUserBody(Guid? UserId);
    public record RescheduleGanttTaskBody(DateOnly StartDate, DateOnly EndDate);
    public record UpdateGanttTaskProgressBody(int ProgressPercent);
    public record AddGanttTaskDependencyBody(Guid PredecessorTaskId, DependencyType Type = DependencyType.FinishToStart, int LagDays = 0);
}
