namespace RobloxAccountManager.Core.Models;

/// <summary>
/// Domain model representing a managed Roblox account.
/// </summary>
public sealed class Account
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long? RobloxUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public byte[] EncryptedSecret { get; set; } = Array.Empty<byte>();
    public byte[]? SecretEntropy { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
