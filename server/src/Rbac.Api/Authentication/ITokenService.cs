using Rbac.Api.Domain;

namespace Rbac.Api.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>Creates a cryptographically random refresh token value.</summary>
    string CreateRefreshTokenValue();

    /// <summary>Hashes a refresh token for storage. The plain value is never saved.</summary>
    string HashRefreshToken(string value);
}
