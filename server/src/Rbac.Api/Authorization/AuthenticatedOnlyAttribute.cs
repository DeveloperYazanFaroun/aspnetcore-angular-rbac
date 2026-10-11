using Microsoft.AspNetCore.Authorization;

namespace Rbac.Api.Authorization;

/// <summary>
/// Marks an endpoint that any signed-in user may call without a specific permission,
/// for example "get my own profile". Using this attribute makes the decision explicit and reviewable.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthenticatedOnlyAttribute : AuthorizeAttribute
{
}
