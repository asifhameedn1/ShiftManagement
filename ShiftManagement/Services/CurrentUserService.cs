using Application.Common.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace ShiftManagement.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public CurrentUserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<string?> GetUsernameAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var username = state.User.Identity?.Name?.ToLowerInvariant();
        if (username != null && username.Contains('\\'))
        {
            username = username[(username.LastIndexOf('\\') + 1)..];
        }
        return username;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated ?? false;
    }
}
