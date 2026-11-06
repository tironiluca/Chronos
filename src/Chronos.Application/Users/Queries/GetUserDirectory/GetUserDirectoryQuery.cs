using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Users.Queries.GetUserDirectory;

public record GetUserDirectoryQuery(Guid CallerOrganizationId) : IRequest<Result<IReadOnlyList<UserDirectoryEntryDto>>>;
