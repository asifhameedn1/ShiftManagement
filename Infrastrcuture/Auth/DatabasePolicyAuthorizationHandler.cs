using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth;

public sealed class DatabasePolicyAuthorizationHandler : AuthorizationHandler<DatabasePolicyRequirement>
{
    private readonly IApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public DatabasePolicyAuthorizationHandler(IApplicationDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DatabasePolicyRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var username = context.User.Identity.Name?.ToLowerInvariant();
        if (string.IsNullOrEmpty(username))
        {
            return;
        }

        // Strip domain prefix if present (e.g. DOMAIN\username -> username)
        if (username.Contains('\\'))
        {
            username = username[(username.LastIndexOf('\\') + 1)..];
        }

        // Fetch employee from the DB (no AsNoTracking, as we might write to it)
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Username == username);

        // If the employee doesn't exist, register them. They start with no department
        // memberships/roles and must be explicitly granted access by an admin.
        if (employee is null)
        {
            var rawName = context.User.Identity.Name ?? username;
            var displayName = rawName.Contains('\\') ? rawName[(rawName.LastIndexOf('\\') + 1)..] : rawName;

            employee = Domain.Entities.Employee.Create(displayName, username);

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return;
        }

        if (!employee.IsActive)
        {
            return;
        }

        // Check if the user has any one of the required permissions, in any department
        foreach (var permissionName in requirement.PermissionNames)
        {
            if (await _permissionService.HasPermissionAsync(username, permissionName))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
