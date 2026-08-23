namespace Domain.Entities;

/// <summary>Represents an organisational department that owns one or more locations.</summary>
public sealed class Department
{
    private Department() { } // EF Core constructor

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();
    private readonly List<Location> _locations = [];

    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();
    private readonly List<Employee> _employees = [];

    public static Department Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
