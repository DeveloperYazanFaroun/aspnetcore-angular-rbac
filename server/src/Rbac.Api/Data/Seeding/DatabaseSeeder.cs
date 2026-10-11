using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Domain;
using P = Rbac.Api.Authorization.Permissions;

namespace Rbac.Api.Data.Seeding;

/// <summary>
/// Keeps the permission table in sync with the code catalogue and creates demo data on first run.
/// </summary>
public sealed class DatabaseSeeder(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider,
    ILogger<DatabaseSeeder> logger)
{
    /// <summary>Password for every demo account. Demo data only.</summary>
    public const string DemoPassword = "Passw0rd!";

    private static readonly RoleSeed[] DefaultRoles =
    [
        new("Accountant", "Creates, edits and submits invoices",
            [P.Invoices.View, P.Invoices.Create, P.Invoices.Edit, P.Invoices.Delete, P.Invoices.Submit, P.Invoices.Export]),
        new("Approver", "Reviews submitted invoices", [P.Invoices.View, P.Invoices.Approve]),
        new("Viewer", "Read-only access to invoices", [P.Invoices.View]),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SynchronizePermissionsAsync(cancellationToken);

        if (!await dbContext.Tenants.AnyAsync(cancellationToken))
        {
            await SeedDemoDataAsync(cancellationToken);
        }

        await SynchronizeAdministratorRolesAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts new permission codes and updates descriptions. Removed codes are not deleted automatically:
    /// remove them in a reviewed migration after checking which roles still use them.
    /// </summary>
    private async Task SynchronizePermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        foreach (var definition in P.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Group = definition.Group;
                permission.Description = definition.Description;
            }
            else
            {
                dbContext.Permissions.Add(new Permission
                {
                    Code = definition.Code,
                    Group = definition.Group,
                    Description = definition.Description,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The system Administrator role of every tenant always has every permission.</summary>
    private async Task SynchronizeAdministratorRolesAsync(CancellationToken cancellationToken)
    {
        var allPermissionIds = await dbContext.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);

        var adminRoles = await dbContext.Roles
            .Where(r => r.IsSystem && r.Name == ApplicationRole.AdministratorRoleName)
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);

        foreach (var role in adminRoles)
        {
            var missing = allPermissionIds.Except(role.Permissions.Select(rp => rp.PermissionId)).ToList();
            foreach (var permissionId in missing)
            {
                role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding demo tenants, roles, users and invoices");

        var permissionIds = await dbContext.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        var contoso = await CreateTenantAsync("Contoso", permissionIds, cancellationToken);
        var fabrikam = await CreateTenantAsync("Fabrikam", permissionIds, cancellationToken);

        var contosoAccountant = await CreateUserAsync(contoso, "accountant@contoso.test", "Sara Haddad", "Accountant");
        var contosoApprover = await CreateUserAsync(contoso, "approver@contoso.test", "Omar Khalil", "Approver");
        await CreateUserAsync(contoso, "admin@contoso.test", "Lina Nasser", ApplicationRole.AdministratorRoleName);
        await CreateUserAsync(contoso, "viewer@contoso.test", "Kareem Saleh", "Viewer");
        await CreateUserAsync(contoso, "lead@contoso.test", "Maya Odeh", "Accountant", "Approver");

        var fabrikamAccountant = await CreateUserAsync(fabrikam, "accountant@fabrikam.test", "John Miller", "Accountant");
        await CreateUserAsync(fabrikam, "admin@fabrikam.test", "Emma Clark", ApplicationRole.AdministratorRoleName);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var contosoAccountantId = contosoAccountant.Id;

        var invoices = new List<Invoice>
        {
            Invoice.CreateDraft(contoso.Tenant.Id, "INV-0001", "Blue Yonder Airlines", 1250.00m, contosoAccountantId, now.AddDays(-9)),
            Invoice.CreateDraft(contoso.Tenant.Id, "INV-0002", "Northwind Traders", 980.50m, contosoAccountantId, now.AddDays(-6)),
            Invoice.CreateDraft(contoso.Tenant.Id, "INV-0003", "Wide World Importers", 4300.00m, contosoAccountantId, now.AddDays(-3)),
            Invoice.CreateDraft(contoso.Tenant.Id, "INV-0004", "Litware Inc.", 615.75m, contosoAccountantId, now.AddDays(-1)),
            Invoice.CreateDraft(fabrikam.Tenant.Id, "INV-0001", "Adventure Works", 2200.00m, fabrikamAccountant.Id, now.AddDays(-4)),
            Invoice.CreateDraft(fabrikam.Tenant.Id, "INV-0002", "Tailspin Toys", 340.00m, fabrikamAccountant.Id, now.AddDays(-2)),
        };

        // Move some invoices through the workflow so every status is visible.
        invoices[1].Submit();
        invoices[2].Submit();
        invoices[2].Approve(contosoApprover.Id, now.AddDays(-2));
        invoices[4].Submit();

        dbContext.Invoices.AddRange(invoices);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SeededTenant> CreateTenantAsync(
        string name, IReadOnlyDictionary<string, int> permissionIds, CancellationToken cancellationToken)
    {
        var tenant = new Tenant { Name = name };
        dbContext.Tenants.Add(tenant);

        var roles = new Dictionary<string, ApplicationRole>(StringComparer.OrdinalIgnoreCase);

        var administrator = NewRole(tenant.Id, ApplicationRole.AdministratorRoleName, "Full access to the organization", isSystem: true);
        roles[administrator.Name!] = administrator;

        foreach (var seed in DefaultRoles)
        {
            var role = NewRole(tenant.Id, seed.Name, seed.Description, isSystem: false);
            foreach (var code in seed.Permissions)
            {
                role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionIds[code] });
            }

            roles[role.Name!] = role;
        }

        dbContext.Roles.AddRange(roles.Values);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeededTenant(tenant, roles);
    }

    private async Task<ApplicationUser> CreateUserAsync(
        SeededTenant tenant, string email, string fullName, params string[] roleNames)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            TenantId = tenant.Tenant.Id,
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create demo user '{email}': {errors}");
        }

        // Insert role links directly: roles are unique per tenant, while UserManager.AddToRoleAsync
        // looks roles up by name across all tenants.
        foreach (var roleName in roleNames)
        {
            dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = tenant.Roles[roleName].Id });
        }

        await dbContext.SaveChangesAsync();
        return user;
    }

    private static ApplicationRole NewRole(Guid tenantId, string name, string description, bool isSystem) => new()
    {
        TenantId = tenantId,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        Description = description,
        IsSystem = isSystem,
        ConcurrencyStamp = Guid.NewGuid().ToString(),
    };

    private sealed record RoleSeed(string Name, string Description, string[] Permissions);

    private sealed record SeededTenant(Tenant Tenant, IReadOnlyDictionary<string, ApplicationRole> Roles);
}
