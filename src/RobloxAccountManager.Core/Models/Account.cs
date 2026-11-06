namespace RobloxAccountManager.Core.Models;

/// <summary>
/// Domain model representing a managed Roblox account.
/// Non-sensitive metadata is separated from cryptographic secret storage.
/// </summary>
public sealed class Account
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long? RobloxUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Tag { get; set; } = "Main";
    public string? Notes { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Encrypted secret payload (populated via DPAPI in Phase 4; never plaintext in DB)
    public byte[] EncryptedSecret { get; set; } = Array.Empty<byte>();
    public byte[]? SecretEntropy { get; set; }
}
