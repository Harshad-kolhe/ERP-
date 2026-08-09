using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Erp.Modules.Masters.Infrastructure.Configurations;

internal sealed class ParentPartConfiguration : IEntityTypeConfiguration<ParentPart>
{
    private static readonly ValueConverter<ParentPartId, Guid> IdConverter =
        new(id => id.Value, value => new ParentPartId(value));

    private static readonly ValueConverter<AssemblyNodeId, Guid> AssemblyNodeIdConverter =
        new(id => id.Value, value => new AssemblyNodeId(value));

    /// <summary>
    /// Shared with <see cref="ParentPartComponentConfiguration"/> so both ends of
    /// the relationship to the part master map identically.
    /// </summary>
    internal static readonly ValueConverter<PartId, Guid> PartIdConverter =
        new(id => id.Value, value => new PartId(value));

    public void Configure(EntityTypeBuilder<ParentPart> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParentPart");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(IdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.PartId)
            .HasConversion(PartIdConverter);

        builder.Property(p => p.AssemblyNodeId)
            .HasConversion(AssemblyNodeIdConverter);

        builder.Property(p => p.Description).HasMaxLength(255);
        builder.Property(p => p.UnitOfMeasureCode).HasMaxLength(10);
        builder.Property(p => p.DrawingNumber).HasMaxLength(50);
        builder.Property(p => p.Category).HasMaxLength(50);

        builder.Property(p => p.TotalWeightKg).HasPrecision(18, 4);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 4);

        // Real foreign keys, which the legacy pair of tables had none of: both
        // sides of the relationship were part numbers stored as free text, so a
        // typo produced a build whose parent no part matched and nothing rejected
        // it. Restrict rather than cascade — deleting a part that something is
        // built from must fail loudly.
        builder.HasOne<Part>()
            .WithMany()
            .HasForeignKey(p => p.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AssemblyNode>()
            .WithMany()
            .HasForeignKey(p => p.AssemblyNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // The lines belong to the header and are only ever reached through it, so
        // they are loaded through the backing field and deleted with it.
        builder.HasMany(p => p.Components)
            .WithOne()
            .HasForeignKey(c => c.ParentPartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(ParentPart.Components))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // One build per part. The legacy screen allowed a second header row for the
        // same parent number, and the two then disagreed about what the part was
        // made of.
        builder.HasIndex(p => new { p.BusinessUnitId, p.PartId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_ParentPart_BusinessUnit_Part");

        builder.HasIndex(p => new { p.BusinessUnitId, p.AssemblyNodeId })
            .HasDatabaseName("IX_ParentPart_BusinessUnit_AssemblyNode");

        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class ParentPartComponentConfiguration : IEntityTypeConfiguration<ParentPartComponent>
{
    public void Configure(EntityTypeBuilder<ParentPartComponent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParentPartComponent");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ParentPartId)
            .HasConversion(id => id.Value, value => new ParentPartId(value));

        builder.Property(c => c.PartId)
            .HasConversion(ParentPartConfiguration.PartIdConverter);

        builder.Property(c => c.Quantity)
            .HasPrecision(18, ParentPartComponent.QuantityScale)
            .IsRequired();

        builder.Property(c => c.UnitWeightKg).HasPrecision(18, ParentPartComponent.WeightScale);
        builder.Property(c => c.LineWeightKg).HasPrecision(18, ParentPartComponent.WeightScale);
        builder.Property(c => c.Rate).HasPrecision(18, ParentPartComponent.MoneyScale);
        builder.Property(c => c.Amount).HasPrecision(18, ParentPartComponent.MoneyScale);

        builder.Property(c => c.UnitOfMeasureCode).HasMaxLength(10);
        builder.Property(c => c.DrawingNumber).HasMaxLength(50);
        builder.Property(c => c.Remark).HasMaxLength(500);

        builder.HasOne<Part>()
            .WithMany()
            .HasForeignKey(c => c.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // A part may appear on a build once. The legacy screen had no such check,
        // so the same child could be added repeatedly and every copy counted
        // towards the weight and cost totals.
        builder.HasIndex(c => new { c.ParentPartId, c.PartId })
            .IsUnique()
            .HasDatabaseName("UX_ParentPartComponent_ParentPart_Part");

        // The order the lines are read back in.
        builder.HasIndex(c => new { c.ParentPartId, c.LineNumber })
            .HasDatabaseName("IX_ParentPartComponent_ParentPart_LineNumber");
    }
}
