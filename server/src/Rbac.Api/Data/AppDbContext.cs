using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authorization;
using Rbac.Api.Domain;

namespace Rbac.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>
    /// Read by the tenant query filter. It must be a member of the DbContext so EF Core
    /// evaluates it for every context instance (every request) instead of caching one value.
    /// </summary>
    private Guid? CurrentTenantId => currentUser.TenantId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Shorter table names than the Identity defaults ("AspNetUsers", ...).
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenant isolation for business data. Every query on Invoices gets "WHERE TenantId = @current".
        // Identity tables (users, roles) are NOT filtered globally, because sign-in runs before a tenant
        // is known. Services filter them explicitly instead.
        builder.Entity<Invoice>().HasQueryFilter(invoice => invoice.TenantId == CurrentTenantId);
    }
}
