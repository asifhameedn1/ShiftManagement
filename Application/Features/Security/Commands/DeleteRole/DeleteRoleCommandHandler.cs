using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler : IHandler<DeleteRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> HandleAsync(DeleteRoleCommand request, CancellationToken cancellationToken = default)
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

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
        {
            return Result.Failure(Error.NotFound("Role", $"Role with ID '{request.Id}' was not found."));
        }

        if (role.Name == "Admin" || role.Name == "Manager" || role.Name == "Employee")
        {
            return Result.Failure(new Error("Role.System", "System default roles (Admin, Manager, Employee) cannot be deleted."));
        }

        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.Roles.Any(r => r.Id == request.Id), cancellationToken);

        if (hasEmployees)
        {
            return Result.Failure(new Error("Role.Assigned", "This role is currently assigned to one or more employees and cannot be deleted."));
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
