namespace Rbac.Api.Domain;

/// <summary>
/// A long-lived token used to get a new access token. Only a SHA-256 hash is stored.
/// Tokens are rotated on every use; reusing an old token revokes all sessions of the user.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => ExpiresAtUtc <= nowUtc;
}
