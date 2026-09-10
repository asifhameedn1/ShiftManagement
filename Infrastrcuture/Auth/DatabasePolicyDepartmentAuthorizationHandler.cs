using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>
/// Resource-based counterpart to <see cref="DatabasePolicyAuthorizationHandler"/>: checks a
/// permission within a specific department when a department id is supplied as the
/// authorization resource (e.g. via IAuthorizationService.AuthorizeAsync(user, departmentId, policy)).
/// </summary>
public sealed class DatabasePolicyDepartmentAuthorizationHandler : AuthorizationHandler<DatabasePolicyRequirement, Guid>
{
    private readonly IPermissionService _permissionService;

    public DatabasePolicyDepartmentAuthorizationHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DatabasePolicyRequirement requirement,
        Guid departmentId)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var username = context.User.Identity.Name?.ToLowerInvariant();
        if (string.IsNullOrEmpty(username))
        {
            return;
        }

        if (username.Contains('\\'))
        {
            username = username[(username.LastIndexOf('\\') + 1)..];
        }

        foreach (var permissionName in requirement.PermissionNames)
        {
            if (await _permissionService.HasPermissionAsync(username, permissionName, departmentId))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
