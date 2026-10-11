using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Features.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>Returned to the client. The refresh token is sent separately as an HttpOnly cookie.</summary>
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

/// <summary>Internal result of a sign-in or refresh: both tokens.</summary>
public sealed record AuthSession(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
