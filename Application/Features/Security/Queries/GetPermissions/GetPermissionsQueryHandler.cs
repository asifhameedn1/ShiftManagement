using Application.Common.Interfaces;
using Domain.Common;
using Application.Features.Security.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Queries.GetPermissions;

public sealed class GetPermissionsQueryHandler
    : IHandler<GetPermissionsQuery, Result<IReadOnlyList<PermissionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public GetPermissionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result<IReadOnlyList<PermissionDto>>> HandleAsync(
        GetPermissionsQuery request,
        CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure<IReadOnlyList<PermissionDto>>(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Role.Manage", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure<IReadOnlyList<PermissionDto>>(new Error("Security.Unauthorized", "You do not have permission to manage roles and permissions."));
        }

        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PermissionDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PermissionDto>>(permissions);
    }
}
