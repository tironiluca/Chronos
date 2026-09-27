using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Organizations.Commands.CreateDepartment;
using Chronos.Application.Organizations.Queries.GetDepartmentsByOrganization;

namespace Chronos.Api.Endpoints;

public static class DepartmentEndpoints
{
    public static void MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/departments").WithTags("Departments").RequireAuthorization();

        // Admin-only, scoped to the caller's own organization -- an Admin can only ever create a
        // department in (and, via ParentDepartmentId, nest it under) their own org. Mirrors
        // OrganizationEndpoints/UserEndpoints: identity always comes from the token, never the body.
        group.MapPost("/", async (CreateDepartmentBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var command = new CreateDepartmentCommand(user.GetOrganizationId(), body.Name, body.Code, body.ParentDepartmentId);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/departments/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        // Any authenticated user in the org may list its departments -- needed to populate
        // department filters/pickers (e.g. the resource-availability calendar), not just for Admins.
        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetDepartmentsByOrganizationQuery(user.GetOrganizationId()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    public record CreateDepartmentBody(string Name, string Code, Guid? ParentDepartmentId);
}
