using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Departments.Queries.GetDepartmentsWithLocations;

public sealed class GetDepartmentsWithLocationsQueryHandler
    : IHandler<GetDepartmentsWithLocationsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public GetDepartmentsWithLocationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<IReadOnlyList<DepartmentDto>>> HandleAsync(
        GetDepartmentsWithLocationsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.AsNoTracking();

        if (!string.IsNullOrEmpty(request.RequiredPermission))
        {
            var username = await _currentUserService.GetUsernameAsync();
            if (string.IsNullOrEmpty(username))
            {
                return Result.Failure<IReadOnlyList<DepartmentDto>>(
                    new Error("Security.Unauthenticated", "User is not authenticated."));
            }

            var allowedIds = await _permissionService.GetDepartmentIdsWithPermissionAsync(
                username, request.RequiredPermission, cancellationToken);

            query = query.Where(d => allowedIds.Contains(d.Id));
        }

        var departments = await query
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
