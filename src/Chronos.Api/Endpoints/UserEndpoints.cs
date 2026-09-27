using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Users.Commands.AssignUserDepartment;
using Chronos.Application.Users.Commands.PromoteUserRole;
using Chronos.Application.Users.Queries.GetRightsForRole;
using Chronos.Domain.Users;

namespace Chronos.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        // Admin-only: the caller's own organization is always used as the scope, so an Admin can
        // never promote/demote a user belonging to a different organization (see
        // PromoteUserRoleCommandHandler).
        group.MapPatch("/{id:guid}/role", async (Guid id, ChangeUserRoleBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new PromoteUserRoleCommand(user.GetOrganizationId(), id, body.Role));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        // Rights the caller's own role grants -- lets the frontend show/hide actions without
        // hard-coding role names client-side.
        group.MapGet("/me/rights", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new GetRightsForRoleQuery(user.GetRole()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Admin-only, same cross-org shape as role promotion above: a null DepartmentId
        // unassigns the user, and the target department (if any) must belong to the caller's org.
        group.MapPatch("/{id:guid}/department", async (Guid id, AssignUserDepartmentBody body, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new AssignUserDepartmentCommand(user.GetOrganizationId(), id, body.DepartmentId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");
    }

    public record ChangeUserRoleBody(UserRole Role);
    public record AssignUserDepartmentBody(Guid? DepartmentId);
}
