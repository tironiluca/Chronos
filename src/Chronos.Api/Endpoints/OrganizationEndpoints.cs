using MediatR;
using Chronos.Application.Organizations.Commands.RegisterOrganization;

namespace Chronos.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").WithTags("Organizations");

        // Open, like /api/auth/register -- someone has to be able to stand up the first
        // organization before anyone can self-register a user into it. A real deployment should
        // gate this the same way registration would need to be gated (see AuthEndpoints).
        group.MapPost("/", async (RegisterOrganizationBody body, ISender sender) =>
        {
            var command = new RegisterOrganizationCommand(body.Name, body.Code);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/organizations/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });
    }

    public record RegisterOrganizationBody(string Name, string Code);
}
