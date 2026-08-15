namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    Task<string?> GetUsernameAsync();
    Task<bool> IsAuthenticatedAsync();
}
