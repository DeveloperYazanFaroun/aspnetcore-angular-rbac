using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Api.Domain;

namespace Rbac.Api.Data.Configurations;

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
        builder.Property(r => r.Description).HasMaxLength(500);

        // Identity makes role names unique across the whole database.
        // Roles here are per tenant, so the name only has to be unique inside one tenant.
        builder.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName }).IsUnique();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
