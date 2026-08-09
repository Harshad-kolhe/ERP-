using Erp.Modules.Masters.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Modules.Masters.Infrastructure.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Customer");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.CustomerCode).HasMaxLength(50);
        builder.Property(c => c.CustomerName).HasMaxLength(200);
        builder.Property(c => c.Industry).HasMaxLength(100);

        builder.Property(c => c.PrimaryContact).HasMaxLength(100);
        builder.Property(c => c.SecondaryContact).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.AltPhone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.AltEmail).HasMaxLength(150);
        builder.Property(c => c.Website).HasMaxLength(200);

        builder.Property(c => c.BillingAddress).HasMaxLength(500);
        builder.Property(c => c.BillingCity).HasMaxLength(100);
        builder.Property(c => c.BillingState).HasMaxLength(100);
        builder.Property(c => c.BillingCountry).HasMaxLength(100);
        builder.Property(c => c.BillingZipCode).HasMaxLength(20);

        builder.Property(c => c.ShippingAddress).HasMaxLength(500);
        builder.Property(c => c.ShippingCity).HasMaxLength(100);
        builder.Property(c => c.ShippingState).HasMaxLength(100);
        builder.Property(c => c.ShippingCountry).HasMaxLength(100);
        builder.Property(c => c.ShippingZipCode).HasMaxLength(20);

        builder.Property(c => c.TaxId).HasMaxLength(50);
        builder.Property(c => c.Gst).HasMaxLength(15);
        builder.Property(c => c.Pan).HasMaxLength(10);
        builder.Property(c => c.Currency).HasMaxLength(3);
        builder.Property(c => c.TaxCode).HasMaxLength(50);

        // Percentages, not amounts — see SupplierConfiguration.
        builder.Property(c => c.Igst).HasPrecision(9, 4);
        builder.Property(c => c.Cgst).HasPrecision(9, 4);
        builder.Property(c => c.Sgst).HasPrecision(9, 4);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.BusinessUnitId, c.CustomerCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [CustomerCode] IS NOT NULL")
            .HasDatabaseName("UX_Customer_BusinessUnit_CustomerCode");

        builder.HasIndex(c => new { c.BusinessUnitId, c.Status, c.CustomerName })
            .HasDatabaseName("IX_Customer_BusinessUnit_Status_CustomerName");

        builder.Ignore(c => c.DomainEvents);
    }
}
