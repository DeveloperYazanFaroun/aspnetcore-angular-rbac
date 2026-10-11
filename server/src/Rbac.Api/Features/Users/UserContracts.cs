using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Features.Users;

public sealed record UserRoleResponse(Guid Id, string Name);

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<UserRoleResponse> Roles);

public sealed record CreateUserRequest(
    [Required, StringLength(200, MinimumLength = 2)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    IReadOnlyList<Guid>? RoleIds);

public sealed record AssignRolesRequest([Required] IReadOnlyList<Guid> RoleIds);
