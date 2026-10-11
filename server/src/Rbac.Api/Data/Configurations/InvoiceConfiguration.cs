using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Api.Domain;

namespace Rbac.Api.Data.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.Property(i => i.Number).HasMaxLength(30).IsRequired();
        builder.Property(i => i.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(i => new { i.TenantId, i.Number }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
