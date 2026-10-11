namespace Rbac.Api.Authorization;

/// <summary>
/// Claim names used in access tokens. Short JWT names are kept as-is because
/// <c>MapInboundClaims</c> is turned off (see <c>AuthenticationExtensions</c>).
/// </summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string TenantId = "tenant_id";
}
