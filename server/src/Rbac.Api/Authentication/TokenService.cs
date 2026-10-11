using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rbac.Api.Authorization;
using Rbac.Api.Domain;

namespace Rbac.Api.Authentication;

/// <summary>
/// Issues short-lived JWT access tokens.
/// The token carries identity (who you are), not permissions (what you may do):
/// permissions are resolved on the server by <see cref="IPermissionService"/>.
/// </summary>
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();
    private readonly JwtOptions _jwt = options.Value;
    private readonly SigningCredentials _signingCredentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public AccessToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_jwt.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.Subject, user.Id.ToString()),
            new(AppClaimTypes.Email, user.Email ?? string.Empty),
            new(AppClaimTypes.Name, user.FullName),
            new(AppClaimTypes.TenantId, user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Roles are included for display and logging only. Authorization checks use permissions.
        claims.AddRange(roles.Select(role => new Claim(AppClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = _signingCredentials,
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }

    public string CreateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
