namespace Rbac.Api.Authentication;

/// <summary>
/// Settings for issuing and validating access tokens. Bound from the "Jwt" configuration section.
/// Store <see cref="SigningKey"/> in user secrets or a vault, never in source control for real environments.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
