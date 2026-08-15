using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Departments.Queries.GetDepartmentsWithLocations;

public sealed class GetDepartmentsWithLocationsQueryHandler
    : IHandler<GetDepartmentsWithLocationsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentsWithLocationsQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<Result<IReadOnlyList<DepartmentDto>>> HandleAsync(
        GetDepartmentsWithLocationsQuery request,
        CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Name,
                d.Locations
                    .OrderBy(l => l.Name)
                    .Select(l => new LocationDto(
                        l.Id,
                        l.Name,
                        l.Employees.Count(e => e.IsActive)))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DepartmentDto>>(departments);
    }
}
