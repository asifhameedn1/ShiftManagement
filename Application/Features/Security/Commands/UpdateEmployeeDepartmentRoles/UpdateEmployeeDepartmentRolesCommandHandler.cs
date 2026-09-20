using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Security.Commands.UpdateEmployeeDepartmentRoles;

public sealed class UpdateEmployeeDepartmentRolesCommandHandler : IHandler<UpdateEmployeeDepartmentRolesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public UpdateEmployeeDepartmentRolesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<Result> HandleAsync(UpdateEmployeeDepartmentRolesCommand request, CancellationToken cancellationToken = default)
    {
        var currentUsername = await _currentUserService.GetUsernameAsync();
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Result.Failure(new Error("Security.Unauthenticated", "User is not authenticated."));
        }

        var employee = await _context.Employees
            .Include(e => e.DepartmentRoles)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", $"Employee with ID '{request.EmployeeId}' was not found."));
        }

        var desired = request.Assignments
            .SelectMany(a => a.DepartmentIds.Distinct().Select(d => (DepartmentId: d, a.RoleId)))
            .ToHashSet();

        var current = employee.DepartmentRoles
            .Select(dr => (dr.DepartmentId, dr.RoleId))
            .ToHashSet();

        var toRemove = current.Except(desired).ToList();
        var toAdd = desired.Except(current).ToList();

        // Only departments whose assignments actually change need the caller to hold Role.Manage there.
        foreach (var departmentId in toRemove.Concat(toAdd).Select(p => p.DepartmentId).Distinct())
        {
            var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Role.Manage", departmentId, cancellationToken);
            if (!isAuthorized)
            {
                return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage employee roles in one of the selected departments."));
            }
        }

        var departmentIds = toAdd.Select(p => p.DepartmentId).Distinct().ToList();
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var missingDepartment = departmentIds.FirstOrDefault(id => !departments.ContainsKey(id));
        if (missingDepartment != Guid.Empty)
        {
            return Result.Failure(Error.NotFound("Department", $"Department with ID '{missingDepartment}' was not found."));
        }

        var roleIds = toAdd.Select(p => p.RoleId).Distinct().ToList();
        var roles = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var missingRole = roleIds.FirstOrDefault(id => !roles.ContainsKey(id));
        if (missingRole != Guid.Empty)
        {
            return Result.Failure(Error.NotFound("Role", $"Role with ID '{missingRole}' was not found."));
        }

        foreach (var (departmentId, roleId) in toRemove)
        {
            employee.RemoveRole(departmentId, roleId);
        }

        foreach (var (departmentId, roleId) in toAdd)
        {
            // AssignRole returns the newly created join entity so it can be explicitly tracked as
            // Added — without this, EF's graph fix-up mistakes a new entity with a client-generated
            // key for an existing row (since `employee` is already tracked as Unchanged), producing
            // a failing UPDATE instead of an INSERT.
            var departmentRole = employee.AssignRole(departments[departmentId], roles[roleId]);
            if (departmentRole is not null)
            {
                _context.EmployeeDepartmentRoles.Add(departmentRole);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
