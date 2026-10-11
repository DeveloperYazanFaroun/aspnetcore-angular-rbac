using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;

namespace Rbac.Api.Features.PermissionCatalog;

public sealed record PermissionResponse(string Code, string Description);

public sealed record PermissionGroupResponse(string Group, IReadOnlyList<PermissionResponse> Permissions);

/// <summary>The permission catalogue, grouped for the role editor screen.</summary>
[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public IReadOnlyList<PermissionGroupResponse> GetCatalogue() =>
        Permissions.All
            .GroupBy(p => p.Group)
            .Select(group => new PermissionGroupResponse(
                group.Key,
                group.Select(p => new PermissionResponse(p.Code, p.Description)).ToList()))
            .ToList();
}
