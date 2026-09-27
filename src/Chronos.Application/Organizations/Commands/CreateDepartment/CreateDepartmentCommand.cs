using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Organizations.Commands.CreateDepartment;

public record CreateDepartmentCommand(
    Guid OrganizationId,
    string Name,
    string Code,
    Guid? ParentDepartmentId) : IRequest<Result<Guid>>;
