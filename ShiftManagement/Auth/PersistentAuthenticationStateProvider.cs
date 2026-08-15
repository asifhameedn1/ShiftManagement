using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ShiftManagement.Auth;

// Runs inside the interactive Blazor circuit.
// Reads the persisted UserInfo that was captured during SSR to build the ClaimsPrincipal,
// so no Windows auth re-negotiation is needed over WebSocket.
internal sealed class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            _authenticationStateTask = _unauthenticatedTask;
            return;
        }

        var claims = userInfo.IsAuthenticated
            ? new[] { new Claim(ClaimTypes.Name, userInfo.Name) }
            : [];

        var identity = userInfo.IsAuthenticated
            ? new ClaimsIdentity(claims, authenticationType: "Negotiate")
            : new ClaimsIdentity();

        _authenticationStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _authenticationStateTask;
}
