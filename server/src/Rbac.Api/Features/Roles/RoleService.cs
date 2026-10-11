using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authorization;
using Rbac.Api.Common;
using Rbac.Api.Data;
using Rbac.Api.Domain;
using Rbac.Api.Realtime;

namespace Rbac.Api.Features.Roles;

/// <summary>
/// Role management for the current tenant. Roles are tenant data, so every query filters by tenant explicitly.
/// </summary>
public sealed class RoleService(
    AppDbContext dbContext,
    ICurrentUser currentUser,
    PrivilegeEscalationGuard escalationGuard,
    IPermissionChangeNotifier notifier)
{
    public async Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        return await Project(dbContext.Roles.Where(r => r.TenantId == tenantId).OrderBy(r => r.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<RoleResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var role = await Project(dbContext.Roles.Where(r => r.Id == id && r.TenantId == tenantId))
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return NotFound(id);
        }

        return role;
    }

    public async Task<Result<RoleResponse>> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var name = request.Name.Trim();

        var permissionIds = await ValidateAsync(tenantId, name, request.Permissions, roleId: null, cancellationToken);
        if (!permissionIds.IsSuccess)
        {
            return permissionIds.Error!;
        }

        var role = new ApplicationRole
        {
            TenantId = tenantId,
            Name = name,
            NormalizedName = Normalize(name),
            Description = request.Description?.Trim(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        foreach (var permissionId in permissionIds.Value!)
        {
            role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task<Result<RoleResponse>> UpdateAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var role = await dbContext.Roles
            .Include(r => r.Permissions)
            .SingleOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (role is null)
        {
            return NotFound(id);
        }

        if (role.IsSystem)
        {
            return SystemRoleError(role);
        }

        var name = request.Name.Trim();
        var permissionIds = await ValidateAsync(tenantId, name, request.Permissions, role.Id, cancellationToken);
        if (!permissionIds.IsSuccess)
        {
            return permissionIds.Error!;
        }

        role.Name = name;
        role.NormalizedName = Normalize(name);
        role.Description = request.Description?.Trim();

        // Apply only the difference, so unchanged links are left alone.
        var wanted = permissionIds.Value!.ToHashSet();
        foreach (var link in role.Permissions.Where(rp => !wanted.Contains(rp.PermissionId)).ToList())
        {
            role.Permissions.Remove(link);
        }

        foreach (var permissionId in wanted.Except(role.Permissions.Select(rp => rp.PermissionId)).ToList())
        {
            role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await notifier.NotifyAsync(await UsersInRoleAsync(role.Id, cancellationToken));

        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var role = await dbContext.Roles.SingleOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (role is null)
        {
            return NotFound(id);
        }

        if (role.IsSystem)
        {
            return SystemRoleError(role);
        }

        var userCount = await dbContext.UserRoles.CountAsync(ur => ur.RoleId == id, cancellationToken);
        if (userCount > 0)
        {
            return Error.Conflict(
                $"The role '{role.Name}' is assigned to {userCount} user(s). Remove it from those users before deleting it.");
        }

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<List<int>>> ValidateAsync(
        Guid tenantId, string name, IReadOnlyList<string> codes, Guid? roleId, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(name);
        var nameTaken = await dbContext.Roles.AnyAsync(
            r => r.TenantId == tenantId && r.NormalizedName == normalizedName && r.Id != roleId, cancellationToken);

        if (nameTaken)
        {
            return Error.Conflict($"A role named '{name}' already exists. Choose a different role name.");
        }

        var requested = codes.Distinct(StringComparer.Ordinal).ToList();
        var known = await dbContext.Permissions
            .Where(p => requested.Contains(p.Code))
            .Select(p => new { p.Id, p.Code })
            .ToListAsync(cancellationToken);

        var unknown = requested.Except(known.Select(p => p.Code)).ToList();
        if (unknown.Count > 0)
        {
            return Error.Validation($"Unknown permission code(s): {string.Join(", ", unknown)}.");
        }

        var canGrant = await escalationGuard.EnsureCanGrantAsync(requested, cancellationToken);
        if (!canGrant.IsSuccess)
        {
            return canGrant.Error!;
        }

        return known.Select(p => p.Id).ToList();
    }

    private Task<List<Guid>> UsersInRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        dbContext.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync(cancellationToken);

    private IQueryable<RoleResponse> Project(IQueryable<ApplicationRole> roles) =>
        roles.AsNoTracking().Select(r => new RoleResponse(
            r.Id,
            r.Name!,
            r.Description,
            r.IsSystem,
            dbContext.UserRoles.Count(ur => ur.RoleId == r.Id),
            r.Permissions.Select(rp => rp.Permission.Code).OrderBy(code => code).ToList()));

    private static string Normalize(string name) => name.ToUpperInvariant();

    private static Error NotFound(Guid id) => Error.NotFound($"Role '{id}' was not found.");

    private static Error SystemRoleError(ApplicationRole role) =>
        Error.Conflict($"'{role.Name}' is a system role and can't be changed or deleted.");
}
