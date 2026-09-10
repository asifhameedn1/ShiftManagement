using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using System.Security.Claims;

namespace ShiftManagement.Auth;

// Runs on the SERVER both during SSR and for the interactive circuit. During the original SSR
// request, HttpContext carries the Negotiate-authenticated identity, which gets persisted into
// the page. The interactive circuit reconnects over WebSocket, where Negotiate cannot
// re-negotiate (HttpContext.User is anonymous there), so on that side this instead rebuilds the
// identity from the data persisted during SSR.
internal sealed class PersistingAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly Microsoft.Extensions.Logging.ILogger<PersistingAuthenticationStateProvider> _logger;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingAuthenticationStateProvider(PersistentComponentState persistentComponentState, Microsoft.Extensions.Logging.ILogger<PersistingAuthenticationStateProvider> logger)
    {
        _state = persistentComponentState;
        _logger = logger;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveServer);
        _logger.LogWarning("DIAG: provider instance {InstanceId} constructed", GetHashCode());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _authenticationStateTask ??= ResolveAuthenticationStateAsync();
        return _authenticationStateTask;
    }

    private Task<AuthenticationState> ResolveAuthenticationStateAsync()
    {
        var found = _state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo);
        _logger.LogWarning("DIAG: instance {InstanceId} ResolveAuthenticationStateAsync TryTakeFromJson found={Found} name={Name} isAuth={IsAuth}",
            GetHashCode(), found, userInfo?.Name, userInfo?.IsAuthenticated);

        if (found && userInfo is not null)
        {
            var claims = userInfo.IsAuthenticated
                ? new[] { new Claim(ClaimTypes.Name, userInfo.Name) }
                : [];
            var identity = userInfo.IsAuthenticated
                ? new ClaimsIdentity(claims, authenticationType: "Negotiate")
                : new ClaimsIdentity();

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        var baseTask = base.GetAuthenticationStateAsync();
        _logger.LogWarning("DIAG: instance {InstanceId} falling back to base provider", GetHashCode());
        return baseTask;
    }

    private async Task OnPersistingAsync()
    {
        var authenticationState = await GetAuthenticationStateAsync();
        var principal = authenticationState.User;
        var isAuthenticated = principal.Identity?.IsAuthenticated == true;

        _logger.LogWarning("DIAG: instance {InstanceId} OnPersistingAsync persisting name={Name} isAuth={IsAuth}",
            GetHashCode(), principal.Identity?.Name, isAuthenticated);

        _state.PersistAsJson(nameof(UserInfo), new UserInfo
        {
            Name = isAuthenticated ? principal.Identity!.Name ?? string.Empty : string.Empty,
            IsAuthenticated = isAuthenticated
        });
    }

    public void Dispose() => _subscription.Dispose();
}
