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

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();
    private readonly List<Role> _roles = [];

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

    public void AddRole(Role role)
    {
        if (!_roles.Any(r => r.Id == role.Id))
        {
            _roles.Add(role);
        }
    }

    public void RemoveRole(Role role)
    {
        var existing = _roles.FirstOrDefault(r => r.Id == role.Id);
        if (existing is not null)
        {
            _roles.Remove(existing);
        }
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
