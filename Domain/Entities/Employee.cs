namespace Domain.Entities;

/// <summary>Represents an employee in the shift management system.</summary>
public sealed class Employee
{
    private Employee() { } // EF Core constructor

    public Guid Id { get; private set; }

    /// <summary>Full display name (e.g. from AD).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Windows/AD username (DOMAIN\username or UPN).</summary>
    public string Username { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>The location this employee is primarily assigned to (nullable).</summary>
    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }

    public IReadOnlyCollection<Shift> Shifts => _shifts.AsReadOnly();
    private readonly List<Shift> _shifts = [];

    public IReadOnlyCollection<EmployeeDepartmentRole> DepartmentRoles => _departmentRoles.AsReadOnly();
    private readonly List<EmployeeDepartmentRole> _departmentRoles = [];

    public static Employee Create(string name, string username, Guid? locationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return new Employee
        {
            Id = Guid.NewGuid(),
            Name = name,
            Username = username.ToLowerInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LocationId = locationId
        };
    }

    public void AssignLocation(Guid locationId) => LocationId = locationId;
    public void UnassignLocation() => LocationId = null;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    /// <summary>
    /// Assigns the role, scoped to the department. Returns the newly created join entity so
    /// callers can explicitly track it as Added — EF Core's graph fix-up otherwise mistakes a
    /// new entity with a client-generated key for an existing row when the Employee is already
    /// tracked as Unchanged, producing a failing UPDATE instead of an INSERT. Returns null if the
    /// employee already had this role in this department.
    /// </summary>
    public EmployeeDepartmentRole? AssignRole(Department department, Role role)
    {
       

        if (_departmentRoles.Any(dr => dr.DepartmentId == department.Id && dr.RoleId == role.Id))
        {
            return null;
        }

        var departmentRole = EmployeeDepartmentRole.Create(Id, department.Id, role.Id);
        _departmentRoles.Add(departmentRole);
        return departmentRole;
    }

    public void RemoveRole(Guid departmentId, Guid roleId)
    {
        var existing = _departmentRoles.FirstOrDefault(dr => dr.DepartmentId == departmentId && dr.RoleId == roleId);
        if (existing is not null)
        {
            _departmentRoles.Remove(existing);
        }       
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
