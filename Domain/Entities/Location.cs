namespace Domain.Entities;

/// <summary>A physical or logical location that belongs to a department and hosts employees.</summary>
public sealed class Location
{
    private Location() { } // EF Core constructor

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;

    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();
    private readonly List<Employee> _employees = [];

    public static Location Create(string name, Guid departmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (departmentId == Guid.Empty) throw new ArgumentException("DepartmentId is required.", nameof(departmentId));

        return new Location
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            DepartmentId = departmentId
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
