using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Api.Domain;

namespace Rbac.Api.Data.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Group).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
    }
}
