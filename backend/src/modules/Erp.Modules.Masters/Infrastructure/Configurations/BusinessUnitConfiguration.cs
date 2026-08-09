using Erp.Modules.Masters.Domain.BusinessUnits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Modules.Masters.Infrastructure.Configurations;

internal sealed class BusinessUnitConfiguration : IEntityTypeConfiguration<BusinessUnit>
{
    public void Configure(EntityTypeBuilder<BusinessUnit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("BusinessUnit");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.BusinessName).HasMaxLength(200);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.ContactNumber).HasMaxLength(30);
        builder.Property(b => b.Email).HasMaxLength(150);
        builder.Property(b => b.Website).HasMaxLength(200);
        builder.Property(b => b.Cin).HasMaxLength(21);
        builder.Property(b => b.Gstn).HasMaxLength(15);
        builder.Property(b => b.Pan).HasMaxLength(10);
        builder.Property(b => b.StateCode).HasMaxLength(10);
        builder.Property(b => b.StateName).HasMaxLength(100);

        // No BusinessUnitId in this key: the table is not tenant-scoped, so the name
        // is unique system-wide rather than per tenant. See BusinessUnit.
        builder.HasIndex(b => b.BusinessName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [BusinessName] IS NOT NULL")
            .HasDatabaseName("UX_BusinessUnit_BusinessName");

        // Every other table's tenancy column holds this value, so it is looked up
        // far more often than it is written.
        builder.HasIndex(b => b.BusinessUnitId)
            .HasDatabaseName("IX_BusinessUnit_BusinessUnitId");

        builder.Ignore(b => b.DomainEvents);
    }
}
