namespace RobloxAccountManager.App.Models;

public sealed class AccountDisplayModel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Tag { get; init; } = "Main";
    public bool IsDefault { get; init; }
    public string LastActive { get; init; } = "Just now";
    public string AvatarPlaceholder { get; init; } = "👤";
}
