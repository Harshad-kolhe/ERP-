using Erp.Api.Domain.UnitsOfMeasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Api.Persistence.Configurations;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UnitOfMeasure");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        // Ten, matching Part.UnitOfMeasureCode â€” the two columns hold the same value
        // and a wider master would let a code in that a part cannot store.
        builder.Property(u => u.Code).HasMaxLength(10).IsRequired();

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();

        builder.Property(u => u.BaseUnitCode).HasMaxLength(10);

        // (18,6), the quantity scale used throughout. A conversion factor multiplies
        // a quantity, so anything coarser rounds before the quantity is even stored.
        builder.Property(u => u.ConversionToBase).HasPrecision(18, 6);

        builder.HasIndex(u => u.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_UnitOfMeasure_Code");

        builder.Ignore(u => u.BaseCode);

        builder.Ignore(u => u.FactorToBase);

        builder.Ignore(u => u.DomainEvents);
    }
}
