using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth;

public sealed class DatabasePolicyAuthorizationHandler : AuthorizationHandler<DatabasePolicyRequirement>
{
    private readonly IApplicationDbContext _context;

    public DatabasePolicyAuthorizationHandler(IApplicationDbContext context)
    {
        _context = context;
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

        // Fetch employee with their roles and permissions from the DB (no AsNoTracking, as we might write to it)
        var employee = await _context.Employees
            .Include(e => e.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(e => e.Username == username);

        // If the employee doesn't exist, register them and assign the "Employee" role
        if (employee is null)
        {
            var rawName = context.User.Identity.Name ?? username;
            var displayName = rawName.Contains('\\') ? rawName[(rawName.LastIndexOf('\\') + 1)..] : rawName;
            
            employee = Domain.Entities.Employee.Create(displayName, username);
            
            var employeeRole = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == "Employee");
                
            if (employeeRole is not null)
            {
                employee.AddRole(employeeRole);
            }
            
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }
        else if (!employee.IsActive)
        {
            return;
        }
        else if (!employee.Roles.Any())
        {
            // If the employee exists but has no roles assigned, automatically assign the "Employee" role
            var employeeRole = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == "Employee");
                
            if (employeeRole is not null)
            {
                employee.AddRole(employeeRole);
                await _context.SaveChangesAsync();
            }
        }

        // Check if the user has the required permission
        var hasPermission = employee.Roles
            .SelectMany(r => r.Permissions)
            .Any(p => p.Name.Equals(requirement.PermissionName, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
