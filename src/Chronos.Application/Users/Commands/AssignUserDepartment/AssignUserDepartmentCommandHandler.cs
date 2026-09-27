using Chronos.Application.Common;
using Chronos.Application.Organizations;
using MediatR;

namespace Chronos.Application.Users.Commands.AssignUserDepartment;

public class AssignUserDepartmentCommandHandler : IRequestHandler<AssignUserDepartmentCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public AssignUserDepartmentCommandHandler(IUserRepository userRepository, IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(AssignUserDepartmentCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        // Same failure for "not found" and "belongs to another organization" -- mirrors
        // PromoteUserRoleCommandHandler: never let the response reveal whether a user id exists
        // in someone else's org.
        if (user is null || user.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"User '{request.UserId}' was not found.");

        if (request.DepartmentId is { } departmentId)
        {
            var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
            if (department is null || department.OrganizationId != request.CallerOrganizationId)
                return Result.Failure($"Department '{departmentId}' was not found.");
        }

        user.AssignDepartment(request.DepartmentId);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
