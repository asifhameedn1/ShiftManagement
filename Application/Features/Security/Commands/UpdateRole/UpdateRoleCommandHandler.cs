using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler : IHandler<UpdateRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public UpdateRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result> HandleAsync(UpdateRoleCommand request, CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Role.Manage", cancellationToken: cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage roles."));
        }

        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
        {
            return Result.Failure(Error.NotFound("Role", $"Role with ID '{request.Id}' was not found."));
        }

        var isSystemRole = role.Name == "Admin" || role.Name == "Manager" || role.Name == "Employee";
        if (isSystemRole && role.Name != request.Name.Trim())
        {
            return Result.Failure(new Error("Role.System", "System default roles (Admin, Manager, Employee) cannot be renamed."));
        }

        if (role.Name.ToLower() != request.Name.Trim().ToLower())
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower() && r.Id != request.Id, cancellationToken);
            if (exists)
            {
                return Result.Failure(Error.Conflict("Role", $"A role with name '{request.Name}' already exists."));
            }
            role.UpdateName(request.Name);
        }

        role.ClearPermissions();

        if (request.PermissionIds != null && request.PermissionIds.Any())
        {
            var permissions = await _context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                role.AddPermission(permission);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
