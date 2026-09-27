using Chronos.Application.Common;
using Chronos.Domain.Organizations;
using MediatR;

namespace Chronos.Application.Organizations.Commands.CreateDepartment;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public CreateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _departmentRepository.GetByCodeAsync(request.OrganizationId, code, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>($"Department code '{code}' is already registered for this organization.");

        if (request.ParentDepartmentId is { } parentId)
        {
            var parent = await _departmentRepository.GetByIdAsync(parentId, cancellationToken);

            // Same failure for "not found" and "belongs to another organization" -- mirrors
            // PromoteUserRoleCommandHandler's cross-org check, same reasoning: never let the
            // response reveal whether a department id exists in someone else's org.
            if (parent is null || parent.OrganizationId != request.OrganizationId)
                return Result.Failure<Guid>($"Parent department '{parentId}' was not found.");
        }

        var department = new Department(request.OrganizationId, request.Name, code, request.ParentDepartmentId);

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _departmentRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(department.Id);
    }
}
