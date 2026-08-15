using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public sealed class DatabasePolicyRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }

    public DatabasePolicyRequirement(string permissionName)
    {
        PermissionName = permissionName;
    }
}
