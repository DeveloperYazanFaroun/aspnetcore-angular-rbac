namespace Rbac.Api.Authorization;

/// <summary>
/// Answers "which permissions does this user have right now?".
/// Permissions are resolved on the server (not stored in the token), so role changes apply immediately.
/// </summary>
public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>Removes cached permissions so the next request reads fresh data.</summary>
    void Invalidate(IEnumerable<Guid> userIds);
}
