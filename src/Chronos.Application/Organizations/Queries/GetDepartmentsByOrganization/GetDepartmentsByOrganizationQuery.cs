using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Organizations.Queries.GetDepartmentsByOrganization;

public record GetDepartmentsByOrganizationQuery(Guid OrganizationId) : IRequest<Result<IReadOnlyList<DepartmentDto>>>;
