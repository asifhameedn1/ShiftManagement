using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

public sealed class DatabasePolicyProvider : DefaultAuthorizationPolicyProvider
{
    public DatabasePolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // First check if there is a statically registered policy in options
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        // If not, dynamically build a policy targeting this permission name.
        // A comma-separated policy name (e.g. "Employee.View,Employee.Manage")
        // is treated as "user needs any one of these permissions".
        var permissionNames = policyName
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new DatabasePolicyRequirement(permissionNames))
            .Build();
    }
}
