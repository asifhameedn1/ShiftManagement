namespace Domain.Entities;

public sealed class Permission
{
    private Permission() { } // EF Core constructor

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();
    private readonly List<Role> _roles = [];

    public static Permission Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
    }
}
