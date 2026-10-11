using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Features.Roles;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    IReadOnlyList<string> Permissions);

public sealed record SaveRoleRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    [Required] IReadOnlyList<string> Permissions);
