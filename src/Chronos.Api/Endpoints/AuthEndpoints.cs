using MediatR;
using Chronos.Application.Users.Commands.Login;
using Chronos.Application.Users.Commands.RegisterUser;

namespace Chronos.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // Open registration, keyed only by an existing OrganizationId. Fine for this scaffold;
        // a real deployment should gate this behind an invite/admin flow rather than letting
        // anyone self-register into any organization by guessing its id.
        group.MapPost("/register", async (RegisterUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/users/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/login", async (LoginCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        });
    }
}
