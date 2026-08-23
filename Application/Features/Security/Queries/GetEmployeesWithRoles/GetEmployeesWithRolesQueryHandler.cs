using Application.Common.Interfaces;
using Domain.Common;
using Application.Features.Security.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Queries.GetEmployeesWithRoles;

public sealed class GetEmployeesWithRolesQueryHandler
    : IHandler<GetEmployeesWithRolesQuery, Result<IReadOnlyList<EmployeeWithRolesDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public GetEmployeesWithRolesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<IReadOnlyList<EmployeeWithRolesDto>>> HandleAsync(
        GetEmployeesWithRolesQuery request,
        CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<EmployeeWithRolesDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Role.Manage", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<EmployeeWithRolesDto>>(new Error("Security.Unauthorized", "You do not have permission to manage roles and permissions."));
        }

        var employeesRaw = await _context.Employees
            .AsNoTracking()
            .Include(e => e.DepartmentRoles)
                .ThenInclude(dr => dr.Department)
            .Include(e => e.DepartmentRoles)
                .ThenInclude(dr => dr.Role)
                    .ThenInclude(r => r.Permissions)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        // Grouped in memory (not translated to SQL) to keep the nested projection simple.
        var employees = employeesRaw
            .Select(e => new EmployeeWithRolesDto(
                e.Id,
                e.Name,
                e.Username,
                e.DepartmentRoles
                    .GroupBy(dr => dr.Department)
                    .Select(g => new DepartmentRoleAssignmentDto(
                        g.Key.Id,
                        g.Key.Name,
                        g.Select(dr => new RoleDto(
                            dr.Role.Id,
                            dr.Role.Name,
                            dr.Role.Permissions.Select(p => new PermissionDto(p.Id, p.Name)).ToList()
                        )).ToList()
                    ))
                    .ToList()
            ))
            .ToList();

        return Result.Success<IReadOnlyList<EmployeeWithRolesDto>>(employees);
    }
}
