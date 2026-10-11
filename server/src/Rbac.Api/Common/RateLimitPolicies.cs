namespace Rbac.Api.Common;

public static class RateLimitPolicies
{
    /// <summary>Limits sign-in and refresh attempts per client IP address.</summary>
    public const string Authentication = "authentication";
}
