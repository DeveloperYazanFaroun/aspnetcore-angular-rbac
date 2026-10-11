using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rbac.Api.Common;

namespace Rbac.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Authentication)]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    public const string RefreshCookieName = "rbac_refresh";

    /// <summary>Signs in with email and password.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        return StartSession(result.Value!);
    }

    /// <summary>Uses the refresh cookie to get a new access token.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        if (!result.IsSuccess)
        {
            Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expiresAtUtc: null));
            return this.ToProblem(result.Error!);
        }

        return StartSession(result.Value!);
    }

    /// <summary>Revokes the refresh token and removes the cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.SignOutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expiresAtUtc: null));
        return NoContent();
    }

    private TokenResponse StartSession(AuthSession session)
    {
        Response.Cookies.Append(
            RefreshCookieName, session.RefreshToken, RefreshCookieOptions(session.RefreshTokenExpiresAtUtc));
        return new TokenResponse(session.AccessToken, session.AccessTokenExpiresAtUtc);
    }

    /// <summary>
    /// HttpOnly: JavaScript can't read it (protects against XSS).
    /// SameSite=Strict: the browser doesn't send it from other sites (protects against CSRF).
    /// Path: only sent to the auth endpoints.
    /// </summary>
    private static CookieOptions RefreshCookieOptions(DateTime? expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expiresAtUtc is null ? null : new DateTimeOffset(expiresAtUtc.Value, TimeSpan.Zero),
    };
}
