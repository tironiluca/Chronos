using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    Guid OrganizationId,
    string Name,
    string Code,
    DateOnly StartDate) : IRequest<Result<Guid>>;
