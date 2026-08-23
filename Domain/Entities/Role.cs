namespace Domain.Entities;

public sealed class Role
{
    private Role() { } // EF Core constructor

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();
    private readonly List<Permission> _permissions = [];

    public static Role Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void ClearPermissions()
    {
        _permissions.Clear();
    }

    public void AddPermission(Permission permission)
    {
        if (!_permissions.Any(p => p.Id == permission.Id))
        {
            _permissions.Add(permission);
        }
    }

    public void RemovePermission(Permission permission)
    {
        var existing = _permissions.FirstOrDefault(p => p.Id == permission.Id);
        if (existing is not null)
        {
            _permissions.Remove(existing);
        }
    }
}
