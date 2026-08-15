using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Commands.UpdateEmployeeRoles;

public sealed class UpdateEmployeeRolesCommandHandler : IHandler<UpdateEmployeeRolesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEmployeeRolesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> HandleAsync(UpdateEmployeeRolesCommand request, CancellationToken cancellationToken = default)
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
            return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage employee roles."));
        }

        var employee = await _context.Employees
            .Include(e => e.Roles)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", $"Employee with ID '{request.EmployeeId}' was not found."));
        }

        var targetRoles = await _context.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var rolesToRemove = employee.Roles.Where(er => !targetRoles.Any(tr => tr.Id == er.Id)).ToList();
        foreach (var role in rolesToRemove)
        {
            employee.RemoveRole(role);
        }

        foreach (var role in targetRoles)
        {
            employee.AddRole(role);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
