using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Projects.Commands.CreateProject;

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
    }

    public record CreateProjectBody(string Name, string Code, DateOnly StartDate);
}
