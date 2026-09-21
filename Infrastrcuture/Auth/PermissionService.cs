using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth;

public sealed class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _context;

    public PermissionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(
        string username,
        string permissionName,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .Where(e => e.Username == username && e.IsActive)
            .SelectMany(e => e.DepartmentRoles);

        if (departmentId.HasValue)
        {
            query = query.Where(dr => dr.DepartmentId == departmentId.Value);
        }

        return await query
            .SelectMany(dr => dr.Role.Permissions)
            .AnyAsync(p => p.Name == permissionName, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetDepartmentIdsWithPermissionAsync(
        string username,
        string permissionName,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.Username == username && e.IsActive)
            .SelectMany(e => e.DepartmentRoles)
            .Where(dr => dr.Role.Permissions.Any(p => p.Name == permissionName))
            .Select(dr => dr.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
