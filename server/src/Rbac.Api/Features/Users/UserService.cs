using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authorization;
using Rbac.Api.Common;
using Rbac.Api.Data;
using Rbac.Api.Domain;
using Rbac.Api.Features.Auth;
using Rbac.Api.Realtime;

namespace Rbac.Api.Features.Users;

/// <summary>
/// User management for the current tenant: create users, assign roles, activate and deactivate.
/// </summary>
public sealed class UserService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    PrivilegeEscalationGuard escalationGuard,
    IPermissionChangeNotifier notifier,
    AuthService authService)
{
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        return await Project(dbContext.Users.Where(u => u.TenantId == tenantId).OrderBy(u => u.FullName))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<UserResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var user = await Project(dbContext.Users.Where(u => u.Id == id && u.TenantId == tenantId))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound(id);
        }

        return user;
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Error.Conflict($"A user with the email '{email}' already exists. Use a different email address.");
        }

        var roleIds = request.RoleIds ?? Array.Empty<Guid>();
        var rolesCheck = await ValidateRolesAsync(tenantId, roleIds, cancellationToken);
        if (!rolesCheck.IsSuccess)
        {
            return rolesCheck.Error!;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            TenantId = tenantId,
        };

        // UserManager uses the same DbContext, so both saves share one transaction.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Error.Validation(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        foreach (var roleId in roleIds.Distinct())
        {
            dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<Result<UserResponse>> AssignRolesAsync(
        Guid id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;

        if (id == currentUser.RequiredUserId)
        {
            return Error.Forbidden("You can't change your own roles. Ask another administrator.");
        }

        var userExists = await dbContext.Users.AnyAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);
        if (!userExists)
        {
            return NotFound(id);
        }

        var rolesCheck = await ValidateRolesAsync(tenantId, request.RoleIds, cancellationToken);
        if (!rolesCheck.IsSuccess)
        {
            return rolesCheck.Error!;
        }

        var wanted = request.RoleIds.ToHashSet();

        var lastAdminCheck = await EnsureNotLastAdministratorAsync(tenantId, id, keepsAdminRole: wanted, cancellationToken);
        if (!lastAdminCheck.IsSuccess)
        {
            return lastAdminCheck.Error!;
        }
        var current = await dbContext.UserRoles.Where(ur => ur.UserId == id).ToListAsync(cancellationToken);

        dbContext.UserRoles.RemoveRange(current.Where(ur => !wanted.Contains(ur.RoleId)));
        foreach (var roleId in wanted.Except(current.Select(ur => ur.RoleId)))
        {
            dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = id, RoleId = roleId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await notifier.NotifyAsync([id]);

        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<UserResponse>> DeactivateAsync(Guid id, CancellationToken cancellationToken) =>
        SetActiveAsync(id, isActive: false, cancellationToken);

    public Task<Result<UserResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken) =>
        SetActiveAsync(id, isActive: true, cancellationToken);

    private async Task<Result<UserResponse>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;

        if (id == currentUser.RequiredUserId)
        {
            return Error.Forbidden("You can't activate or deactivate your own account.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);
        if (user is null)
        {
            return NotFound(id);
        }

        if (!isActive)
        {
            var lastAdminCheck = await EnsureNotLastAdministratorAsync(tenantId, id, keepsAdminRole: null, cancellationToken);
            if (!lastAdminCheck.IsSuccess)
            {
                return lastAdminCheck.Error!;
            }
        }

        user.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!isActive)
        {
            // Effective immediately: refresh tokens stop working and cached permissions are cleared,
            // so even a still-valid access token can't call permission-protected endpoints.
            await authService.RevokeAllSessionsAsync(id, cancellationToken);
        }

        await notifier.NotifyAsync([id]);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Every organization keeps at least one active Administrator, so nobody can lock the tenant out.
    /// <paramref name="keepsAdminRole"/> is the user's new role set, or null when the user is being deactivated.
    /// </summary>
    private async Task<Result> EnsureNotLastAdministratorAsync(
        Guid tenantId, Guid userId, IReadOnlySet<Guid>? keepsAdminRole, CancellationToken cancellationToken)
    {
        var adminRoleId = await dbContext.Roles
            .Where(r => r.TenantId == tenantId && r.IsSystem && r.Name == ApplicationRole.AdministratorRoleName)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (adminRoleId is null || keepsAdminRole?.Contains(adminRoleId.Value) == true)
        {
            return Result.Success();
        }

        var activeAdmins = await (
                from userRole in dbContext.UserRoles
                join user in dbContext.Users on userRole.UserId equals user.Id
                where userRole.RoleId == adminRoleId && user.IsActive
                select user.Id)
            .ToListAsync(cancellationToken);

        return activeAdmins.Count == 1 && activeAdmins[0] == userId
            ? Error.Conflict("This user is the last active Administrator of your organization. Make someone else an Administrator first.")
            : Result.Success();
    }

    /// <summary>Roles must belong to this tenant, and the caller must hold every permission they grant.</summary>
    private async Task<Result> ValidateRolesAsync(Guid tenantId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken)
    {
        var requested = roleIds.Distinct().ToList();
        var found = await dbContext.Roles
            .Where(r => requested.Contains(r.Id) && r.TenantId == tenantId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var missing = requested.Except(found).ToList();
        if (missing.Count > 0)
        {
            return Error.Validation($"These roles were not found in your organization: {string.Join(", ", missing)}.");
        }

        var granted = await dbContext.RolePermissions
            .Where(rp => requested.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await escalationGuard.EnsureCanGrantAsync(granted, cancellationToken);
    }

    private IQueryable<UserResponse> Project(IQueryable<ApplicationUser> users) =>
        users.AsNoTracking().Select(u => new UserResponse(
            u.Id,
            u.FullName,
            u.Email!,
            u.IsActive,
            (from userRole in dbContext.UserRoles
             join role in dbContext.Roles on userRole.RoleId equals role.Id
             where userRole.UserId == u.Id
             orderby role.Name
             select new UserRoleResponse(role.Id, role.Name!)).ToList()));

    private static Error NotFound(Guid id) => Error.NotFound($"User '{id}' was not found.");
}
