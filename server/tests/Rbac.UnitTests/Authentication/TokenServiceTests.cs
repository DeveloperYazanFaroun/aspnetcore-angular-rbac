using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Rbac.Api.Authentication;
using Rbac.Api.Authorization;
using Rbac.Api.Domain;

namespace Rbac.UnitTests.Authentication;

public sealed class TokenServiceTests
{
    private readonly TokenService _tokenService = new(
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "unit-test-signing-key-0123456789abcdef",
            AccessTokenLifetimeMinutes = 15,
        }),
        TimeProvider.System);

    [Fact]
    public void Access_token_carries_identity_and_roles_but_not_permissions()
    {
        var user = new ApplicationUser { Email = "sara@contoso.test", FullName = "Sara", TenantId = Guid.NewGuid() };

        var token = new JsonWebToken(_tokenService.CreateAccessToken(user, ["Accountant"]).Value);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.TenantId.ToString(), token.GetClaim(AppClaimTypes.TenantId).Value);
        Assert.Contains(token.Claims, c => c.Type == AppClaimTypes.Role && c.Value == "Accountant");
        Assert.DoesNotContain(token.Claims, c => c.Type == "permission");
    }

    [Fact]
    public void Refresh_tokens_are_random_and_hashed_consistently()
    {
        var first = _tokenService.CreateRefreshTokenValue();
        var second = _tokenService.CreateRefreshTokenValue();

        Assert.NotEqual(first, second);
        Assert.Equal(_tokenService.HashRefreshToken(first), _tokenService.HashRefreshToken(first));
        Assert.NotEqual(first, _tokenService.HashRefreshToken(first));
    }
}
