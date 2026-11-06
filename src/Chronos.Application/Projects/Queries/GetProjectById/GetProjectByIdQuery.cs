using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid CallerOrganizationId, Guid ProjectId) : IRequest<Result<ProjectDto>>;
