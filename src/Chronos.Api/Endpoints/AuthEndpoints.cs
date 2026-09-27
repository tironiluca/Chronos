using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Users.Commands.ChangePassword;
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
        //
        // OrganizationId is bound as a string here, not Guid, because a malformed value in a
        // Guid-typed parameter fails at JSON deserialization -- before FluentValidation's
        // pipeline behavior ever runs on the command -- and surfaces as an unhandled
        // BadHttpRequestException instead of a clean validation error.
        group.MapPost("/register", async (RegisterUserBody body, ISender sender) =>
        {
            if (!Guid.TryParse(body.OrganizationId, out var organizationId))
                return Results.BadRequest("Organization ID must be a valid GUID.");

            var command = new RegisterUserCommand(organizationId, body.Email, body.DisplayName, body.Password);
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

        group.MapPost("/change-password", async (ChangePasswordBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new ChangePasswordCommand(user.GetUserId(), body.CurrentPassword, body.NewPassword);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization();
    }

    public record RegisterUserBody(string OrganizationId, string Email, string DisplayName, string Password);
    public record ChangePasswordBody(string CurrentPassword, string NewPassword);
}
