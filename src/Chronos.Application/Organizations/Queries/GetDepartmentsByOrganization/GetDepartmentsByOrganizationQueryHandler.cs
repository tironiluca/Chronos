using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Organizations.Queries.GetDepartmentsByOrganization;

public class GetDepartmentsByOrganizationQueryHandler
    : IRequestHandler<GetDepartmentsByOrganizationQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentsByOrganizationQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result<IReadOnlyList<DepartmentDto>>> Handle(
        GetDepartmentsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetByOrganizationAsync(request.OrganizationId, cancellationToken);

        var dtos = departments
            .Select(d => new DepartmentDto(d.Id, d.OrganizationId, d.Name, d.Code, d.ParentDepartmentId))
            .ToList();

        return Result.Success<IReadOnlyList<DepartmentDto>>(dtos);
    }
}
