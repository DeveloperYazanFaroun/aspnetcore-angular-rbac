using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Api.Domain;

namespace Rbac.Api.Data.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.HasOne(u => u.Tenant).WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => u.TenantId);
    }
}
