using Erp.Modules.Masters.Domain.Parts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Modules.Masters.Infrastructure.Configurations;

internal sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Part");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PartId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.PartNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.OriginalPartNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(p => p.UnitOfMeasureCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.HsnCode).HasMaxLength(10);

        builder.Property(p => p.DrawingNumber).HasMaxLength(50);

        // ---- Legacy Part Master attributes.

        builder.Property(p => p.ItemNumber).HasMaxLength(50);

        // Unicode and long. The legacy column was widened to nvarchar(2000) after a
        // varchar one silently folded engineering symbols (Ω, µ, ×, Ø) into their
        // Latin look-alikes; the comment on that table says not to downgrade it, and
        // this does not.
        builder.Property(p => p.TechnicalSpecification).HasMaxLength(2000);

        builder.Property(p => p.Moc).HasMaxLength(50);

        builder.Property(p => p.PartCategoryCode).HasMaxLength(50);

        builder.Property(p => p.PartType).HasMaxLength(100);

        builder.Property(p => p.FormCategory).HasMaxLength(50);

        builder.Property(p => p.PurchaseUomCode).HasMaxLength(10);

        builder.Property(p => p.SellingUomCode).HasMaxLength(10);

        builder.Property(p => p.MaterialType).HasMaxLength(50);

        builder.Property(p => p.SeriesCode).HasMaxLength(50);

        builder.Property(p => p.PartRevisionNo).HasMaxLength(10);

        builder.Property(p => p.SourceCode).HasMaxLength(50);

        // (18,4) throughout, the convention for every quantity in this system. The
        // precision is explicit because SQL Server's silent default of (18,2) would
        // round a four-decimal weight on the way in.
        builder.Property(p => p.WeightKg).HasPrecision(18, 4);

        builder.Property(p => p.MinimumStockLevel).HasPrecision(18, 4);

        builder.Property(p => p.RevisionRemark).HasMaxLength(500);

        builder.Property(p => p.HoldRemark).HasMaxLength(500);

        builder.Property(p => p.InactiveRemark).HasMaxLength(500);

        // Stored as text, not an integer code. A DBA reading this table sees
        // 'Approved', not '02' — the legacy convention that produced 277 bare
        // occurrences of the literal "01" across the codebase.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Part numbers are unique per business unit. Filtered on IsDeleted so a
        // number belonging to a soft-deleted part can be issued again.
        builder.HasIndex(p => new { p.BusinessUnitId, p.PartNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Part_BusinessUnit_PartNumber");

        // Covers the default grid ordering.
        builder.HasIndex(p => new { p.BusinessUnitId, p.Status, p.PartNumber })
            .HasDatabaseName("IX_Part_BusinessUnit_Status_PartNumber");

        // "Show me every revision of this part" — the query the revision chain
        // exists to answer, and one that would otherwise scan the whole master.
        builder.HasIndex(p => new { p.BusinessUnitId, p.OriginalPartNumber })
            .HasDatabaseName("IX_Part_BusinessUnit_OriginalPartNumber");

        builder.Ignore(p => p.DomainEvents);
    }
}
