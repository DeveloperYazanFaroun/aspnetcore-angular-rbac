using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Api.Domain;

namespace Rbac.Api.Data.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
