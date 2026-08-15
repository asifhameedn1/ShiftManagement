namespace ShiftManagement.Auth;

public sealed class UserInfo
{
    public required string Name { get; init; }
    public required bool IsAuthenticated { get; init; }
}
