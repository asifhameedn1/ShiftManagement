using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>
/// Satisfied when the current user holds ANY one of the given permission names.
/// A policy name of "Employee.View,Employee.Manage" is split into two permission
/// names here, so either permission alone is sufficient.
/// </summary>
public sealed class DatabasePolicyRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> PermissionNames { get; }

    public DatabasePolicyRequirement(params string[] permissionNames)
    {
        PermissionNames = permissionNames;
    }
}
