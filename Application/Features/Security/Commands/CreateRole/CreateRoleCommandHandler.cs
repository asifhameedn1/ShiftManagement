using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Commands.CreateRole;

public sealed class CreateRoleCommandHandler : IHandler<CreateRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> HandleAsync(CreateRoleCommand request, CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var isAuthorized = await _context.Employees
            .Where(e => e.Username == currentUsername && e.IsActive)
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Permissions)
            .AnyAsync(p => p.Name == "Role.Manage", cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage roles."));
        }

        var exists = await _context.Roles.AnyAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower(), cancellationToken);
        if (exists)
        {
            return Result.Failure(Error.Conflict("Role", $"A role with name '{request.Name}' already exists."));
        }

        var role = Role.Create(request.Name);

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

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
