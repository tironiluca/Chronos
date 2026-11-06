using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetProjectsByOrganization;

public record GetProjectsByOrganizationQuery(Guid CallerOrganizationId) : IRequest<Result<IReadOnlyList<ProjectDto>>>;
