using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rbac.Api.Data;

namespace Rbac.Api.Authorization;

/// <summary>
/// Resolves a user's effective permissions: the union of the permissions of all their roles.
/// Results are cached for a short time and invalidated whenever roles or assignments change.
/// </summary>
public sealed class PermissionService(AppDbContext dbContext, IMemoryCache cache) : IPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey(userId), out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        // An inactive user has no permissions, even while their access token is still valid.
        var codes = await (
                from user in dbContext.Users
                where user.Id == userId && user.IsActive
                join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
                join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                select rolePermission.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlySet<string> permissions = codes.ToHashSet(StringComparer.Ordinal);
        cache.Set(CacheKey(userId), permissions, CacheDuration);
        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }

    public void Invalidate(IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            cache.Remove(CacheKey(userId));
        }
    }

    private static string CacheKey(Guid userId) => $"permissions:{userId}";
}
