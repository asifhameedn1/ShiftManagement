using Application.Common.Interfaces;
using Domain.Common;
using Application.Features.Security.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Queries.GetRolesWithPermissions;

public sealed class GetRolesWithPermissionsQueryHandler
    : IHandler<GetRolesWithPermissionsQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRolesWithPermissionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> HandleAsync(
        GetRolesWithPermissionsQuery request,
        CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<RoleDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Role.Manage", cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<RoleDto>>(new Error("Security.Unauthorized", "You do not have permission to manage roles and permissions."));
        }

        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Permissions.Select(p => new PermissionDto(p.Id, p.Name)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RoleDto>>(roles);
    }
}
