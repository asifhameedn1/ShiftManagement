using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
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

        var isAuthorized = await _permissionService.HasPermissionAsync(currentUsername, "Role.Manage", request.DepartmentId, cancellationToken);

        if (!isAuthorized)
        {
            return Result.Failure(new Error("Security.Unauthorized", "You do not have permission to manage employee roles in this department."));
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return Result.Failure(Error.NotFound("Department", $"Department with ID '{request.DepartmentId}' was not found."));
        }

        var employee = await _context.Employees
            .Include(e => e.DepartmentRoles)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", $"Employee with ID '{request.EmployeeId}' was not found."));
        }

        var targetRoles = await _context.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var currentRoleIdsInDepartment = employee.DepartmentRoles
            .Where(dr => dr.DepartmentId == request.DepartmentId)
            .Select(dr => dr.RoleId)
            .ToList();

        var roleIdsToRemove = currentRoleIdsInDepartment.Where(id => !targetRoles.Any(tr => tr.Id == id)).ToList();
        foreach (var roleId in roleIdsToRemove)
        {
            employee.RemoveRole(request.DepartmentId, roleId);
        }

        foreach (var role in targetRoles)
        {
            // AssignRole returns the newly created join entity so it can be explicitly tracked as
            // Added — without this, EF's graph fix-up mistakes a new entity with a client-generated
            // key for an existing row (since `employee` is already tracked as Unchanged), producing
            // a failing UPDATE instead of an INSERT.
            var departmentRole = employee.AssignRole(department, role);
            if (departmentRole is not null)
            {
                _context.EmployeeDepartmentRoles.Add(departmentRole);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
