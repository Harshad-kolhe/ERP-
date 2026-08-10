using Erp.Api.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Api.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Supplier");

        builder.HasKey(s => s.Id);

        // Int identity, so migrated legacy rows keep the ids they already have.
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.SupplierCode).HasMaxLength(50);
        builder.Property(s => s.SupplierName).HasMaxLength(200);
        builder.Property(s => s.SupplierType).HasMaxLength(50);
        builder.Property(s => s.SupplierCatalog).HasMaxLength(100);

        builder.Property(s => s.PrimaryContact).HasMaxLength(100);
        builder.Property(s => s.SecondaryContact).HasMaxLength(100);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.AltPhone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.AltEmail).HasMaxLength(150);
        builder.Property(s => s.Website).HasMaxLength(200);

        builder.Property(s => s.BillingAddress).HasMaxLength(500);
        builder.Property(s => s.BillingCity).HasMaxLength(100);
        builder.Property(s => s.BillingState).HasMaxLength(100);
        builder.Property(s => s.BillingCountry).HasMaxLength(100);
        builder.Property(s => s.BillingZipCode).HasMaxLength(20);

        builder.Property(s => s.ShippingAddress).HasMaxLength(500);
        builder.Property(s => s.ShippingCity).HasMaxLength(100);
        builder.Property(s => s.ShippingState).HasMaxLength(100);
        builder.Property(s => s.ShippingCountry).HasMaxLength(100);
        builder.Property(s => s.ShippingZipCode).HasMaxLength(20);

        builder.Property(s => s.Pan).HasMaxLength(10);
        builder.Property(s => s.TaxId).HasMaxLength(50);
        builder.Property(s => s.GstNo).HasMaxLength(15);
        builder.Property(s => s.BankName).HasMaxLength(150);
        builder.Property(s => s.AccountNumber).HasMaxLength(50);
        builder.Property(s => s.Ifsc).HasMaxLength(11);
        builder.Property(s => s.Swift).HasMaxLength(11);
        builder.Property(s => s.PaymentTerms).HasMaxLength(100);
        builder.Property(s => s.Currency).HasMaxLength(3);
        builder.Property(s => s.TaxCode).HasMaxLength(50);
        builder.Property(s => s.QualityCompliance).HasMaxLength(200);
        builder.Property(s => s.ActiveStatus).HasMaxLength(50);
        builder.Property(s => s.ProgramId).HasMaxLength(50);

        // GST rates are percentages. The context-wide default is money precision
        // (18,2), which would round 2.5% and 12.375% to the same stored scale.
        builder.Property(s => s.Igst).HasPrecision(9, 4);
        builder.Property(s => s.Cgst).HasPrecision(9, 4);
        builder.Property(s => s.Sgst).HasPrecision(9, 4);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Filtered on IsDeleted so a code belonging to a soft-deleted supplier can
        // be issued again.
        builder.HasIndex(s => new { s.BusinessUnitId, s.SupplierCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [SupplierCode] IS NOT NULL")
            .HasDatabaseName("UX_Supplier_BusinessUnit_SupplierCode");

        // Covers the default grid ordering.
        builder.HasIndex(s => new { s.BusinessUnitId, s.Status, s.SupplierName })
            .HasDatabaseName("IX_Supplier_BusinessUnit_Status_SupplierName");

        builder.Ignore(s => s.DomainEvents);
    }
}
