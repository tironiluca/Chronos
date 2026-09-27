using MediatR;
using Chronos.Application.Projects.Commands.CreateProject;

namespace Chronos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapPost("/", async (CreateProjectCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });
    }
}
