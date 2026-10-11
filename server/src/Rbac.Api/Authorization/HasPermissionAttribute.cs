using Microsoft.AspNetCore.Mvc;

namespace Rbac.Api.Authorization;

/// <summary>
/// Requires the signed-in user to have a permission.
/// <code>[HasPermission(Permissions.Invoices.Approve)]</code>
/// Apply it more than once to require several permissions (AND).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permission)
        : base(typeof(PermissionAuthorizationFilter))
    {
        Permission = permission;
        Arguments = [permission];
    }

    public string Permission { get; }
}
