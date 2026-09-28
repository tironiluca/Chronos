using System.Security.Claims;
using MediatR;
using Chronos.Api.Security;
using Chronos.Application.Resources.Queries.GetResourceAvailability;

namespace Chronos.Api.Endpoints;

public static class ResourceAvailabilityEndpoints
{
    public static void MapResourceAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resources").WithTags("Resources").RequireAuthorization();

        // Any authenticated org member may query availability -- it's a read, not a management
        // action, same no-extra-policy treatment as GET /api/boards and GET /api/departments.
        group.MapGet("/availability", async (
            Guid? departmentId, Guid? projectId, DateOnly from, DateOnly to, ClaimsPrincipal user, ISender sender) =>
        {
            var query = new GetResourceAvailabilityQuery(user.GetOrganizationId(), departmentId, projectId, from, to);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}
