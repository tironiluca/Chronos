using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Organizations.Commands.RegisterOrganization;

public record RegisterOrganizationCommand(string Name, string Code) : IRequest<Result<Guid>>;
