using Chronos.Application.Common;
using Chronos.Domain.Organizations;
using MediatR;

namespace Chronos.Application.Organizations.Commands.RegisterOrganization;

public class RegisterOrganizationCommandHandler : IRequestHandler<RegisterOrganizationCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _organizationRepository;

    public RegisterOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterOrganizationCommand request, CancellationToken cancellationToken)
    {
        // FluentValidation's validators are registered in DI but nothing in this codebase wires
        // them into the MediatR pipeline yet (see RegisterUserCommandValidator, which has the
        // same gap), so an invalid request reaches here unvalidated. Check explicitly rather than
        // let it fall through to Organization's constructor, which throws instead of returning
        // a clean Result and would otherwise surface as an unhandled 500.
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<Guid>("Organization name is required.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result.Failure<Guid>("Organization code is required.");

        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _organizationRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>($"Organization code '{code}' is already registered.");

        var organization = new Organization(request.Name, code);

        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _organizationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(organization.Id);
    }
}
