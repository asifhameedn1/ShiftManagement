namespace Domain.Entities;

/// <summary>Assigns a Role to an Employee, scoped to a specific Department.</summary>
public sealed class EmployeeDepartmentRole
{
    private EmployeeDepartmentRole() { } // EF Core constructor

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    public Guid DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public static EmployeeDepartmentRole Create(Guid employeeId, Guid departmentId, Guid roleId)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("EmployeeId is required.", nameof(employeeId));
        if (departmentId == Guid.Empty) throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required.", nameof(roleId));

        return new EmployeeDepartmentRole
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            RoleId = roleId
        };
    }
}
