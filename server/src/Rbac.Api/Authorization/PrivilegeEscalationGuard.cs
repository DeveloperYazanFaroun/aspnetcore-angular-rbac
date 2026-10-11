using Rbac.Api.Common;

namespace Rbac.Api.Authorization;

/// <summary>
/// Stops users from giving out more power than they have. A user with "roles.manage" but without
/// "invoices.approve" must not be able to create a role with "invoices.approve" and assign it to themselves.
/// </summary>
public sealed class PrivilegeEscalationGuard(IPermissionService permissionService, ICurrentUser currentUser)
{
    public async Task<Result> EnsureCanGrantAsync(IEnumerable<string> permissions, CancellationToken cancellationToken)
    {
        var mine = await permissionService.GetPermissionsAsync(currentUser.RequiredUserId, cancellationToken);
        var notHeld = permissions.Where(p => !mine.Contains(p)).Distinct().Order().ToList();

        return notHeld.Count == 0
            ? Result.Success()
            : Error.Forbidden($"You can't grant permissions you don't have yourself: {string.Join(", ", notHeld)}.");
    }
}
