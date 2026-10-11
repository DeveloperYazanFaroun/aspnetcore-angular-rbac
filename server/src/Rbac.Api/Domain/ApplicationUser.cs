using Microsoft.AspNetCore.Identity;

namespace Rbac.Api.Domain;

/// <summary>
/// A person who can sign in. Extends the ASP.NET Core Identity user with a tenant and a display name.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public string FullName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    /// <summary>
    /// A deactivated user can't sign in, can't refresh tokens and loses all permissions immediately.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
