using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using System.Security.Claims;

namespace ShiftManagement.Auth;

// Runs on the SERVER during SSR to capture the Windows identity and persist it
// so the interactive Blazor circuit can read it without needing another auth challenge.
internal sealed class PersistingAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingAuthenticationStateProvider(PersistentComponentState persistentComponentState)
    {
        _state = persistentComponentState;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveServer);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var task = base.GetAuthenticationStateAsync();
        _authenticationStateTask = task;
        return task;
    }

    private async Task OnPersistingAsync()
    {
        // Fall back to base if GetAuthenticationStateAsync was never called before persisting
        var authState = _authenticationStateTask ?? base.GetAuthenticationStateAsync();
        var authenticationState = await authState;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            _state.PersistAsJson(nameof(UserInfo), new UserInfo
            {
                Name = principal.Identity.Name ?? string.Empty,
                IsAuthenticated = true
            });
        }
        else
        {
            _state.PersistAsJson(nameof(UserInfo), new UserInfo
            {
                Name = string.Empty,
                IsAuthenticated = false
            });
        }
    }

    public void Dispose() => _subscription.Dispose();
}
