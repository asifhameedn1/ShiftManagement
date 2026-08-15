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

    public GetEmployeesWithRolesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Role.Manage", cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<EmployeeWithRolesDto>>(new Error("Security.Unauthorized", "You do not have permission to manage roles and permissions."));
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Roles)
                .ThenInclude(r => r.Permissions)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeWithRolesDto(
                e.Id,
                e.Name,
                e.Username,
                e.Roles.Select(r => new RoleDto(
                    r.Id,
                    r.Name,
                    r.Permissions.Select(p => new PermissionDto(p.Id, p.Name)).ToList()
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmployeeWithRolesDto>>(employees);
    }
}
