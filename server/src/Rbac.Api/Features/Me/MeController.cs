using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authorization;
using Rbac.Api.Data;
using Rbac.Api.Domain;

namespace Rbac.Api.Features.Me;

public sealed record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    Guid TenantId,
    string TenantName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>
/// The signed-in user's profile and effective permissions. The Angular client uses this
/// to show or hide UI. The UI is only a convenience: every API endpoint still checks permissions.
/// </summary>
[ApiController]
[Route("api/me")]
[AuthenticatedOnly]
public sealed class MeController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.Tenant)
            .SingleOrDefaultAsync(u => u.Id == currentUser.RequiredUserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized",
                detail: "Your account is no longer active.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetPermissionsAsync(user.Id, cancellationToken);

        return new MeResponse(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.TenantId,
            user.Tenant?.Name ?? string.Empty,
            roles.Order().ToList(),
            permissions.Order().ToList());
    }
}
