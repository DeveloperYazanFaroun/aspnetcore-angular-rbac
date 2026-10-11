using Microsoft.AspNetCore.Identity;

namespace Rbac.Api.Domain;

/// <summary>
/// A named set of permissions that matches a job function, for example "Accountant".
/// Roles are data: tenant administrators create and edit them at runtime.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public const string AdministratorRoleName = "Administrator";

    public ApplicationRole() => Id = Guid.CreateVersion7();

    public Guid TenantId { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// System roles are created by the application and can't be edited or deleted from the API.
    /// </summary>
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> Permissions { get; } = new List<RolePermission>();
}
