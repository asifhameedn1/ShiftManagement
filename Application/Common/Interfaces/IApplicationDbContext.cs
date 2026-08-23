using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext exposed to the Application layer.
/// This keeps Application free of any EF Core dependency.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<Department> Departments { get; }
    DbSet<Location> Locations { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<EmployeeDepartmentRole> EmployeeDepartmentRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
