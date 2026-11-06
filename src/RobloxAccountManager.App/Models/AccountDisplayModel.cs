namespace RobloxAccountManager.App.Models;

public sealed class AccountDisplayModel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Tag { get; set; } = "Main";
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public string LastActive { get; set; } = "Never";
    public string AvatarPlaceholder { get; init; } = "👤";
}
