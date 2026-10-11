namespace Rbac.Api.Domain;

/// <summary>
/// Links a role to one of its permissions (many-to-many).
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    public ApplicationRole Role { get; set; } = null!;

    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
