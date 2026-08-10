using Erp.Api.Domain.Assemblies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Erp.Api.Persistence.Configurations;

internal sealed class AssemblyNodeConfiguration : IEntityTypeConfiguration<AssemblyNode>
{
    /// <summary>
    /// Shared by the key and the self-referencing parent column.
    /// <para>
    /// Declared once rather than inline twice: EF wraps a non-nullable converter
    /// automatically for a nullable property, so the same instance serves both and
    /// the two columns cannot end up mapped differently.
    /// </para>
    /// </summary>
    private static readonly ValueConverter<AssemblyNodeId, Guid> IdConverter =
        new(id => id.Value, value => new AssemblyNodeId(value));

    public void Configure(EntityTypeBuilder<AssemblyNode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AssemblyNode");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasConversion(IdConverter)
            .ValueGeneratedNever();

        builder.Property(n => n.ParentId)
            .HasConversion(IdConverter);

        // Stored as text, not an ordinal. A DBA reading this table sees
        // 'SubAssembly', not '2' â€” and unlike the legacy 'S'/'A'/'SA' codes, no
        // prefix comparison can mistake one level for another.
        builder.Property(n => n.Level)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.ManualCode).HasMaxLength(50);
        builder.Property(n => n.MachineType).HasMaxLength(50);
        builder.Property(n => n.DrivenBy).HasMaxLength(100);
        builder.Property(n => n.DrawingPath).HasMaxLength(500);

        // Unicode and long, for the same reason Part.TechnicalSpecification is:
        // a varchar column folds engineering symbols (Î©, Âµ, Ã—, Ã˜) into look-alikes.
        builder.Property(n => n.TechnicalSpecification).HasMaxLength(2500);

        builder.Property(n => n.Remark).HasMaxLength(500);

        builder.Property(n => n.Quantity).HasPrecision(18, 6);
        builder.Property(n => n.WeightKg).HasPrecision(18, 4);

        // The relationship the legacy table did not have. It stored the parent's
        // *code* as a string with no constraint, so a section could be removed out
        // from under its assemblies and nothing noticed until a report ran.
        //
        // Restrict, not cascade: deleting a section must fail while assemblies hang
        // off it, rather than silently taking the whole branch with it. No
        // navigation property is declared â€” the tree is read by projection, and a
        // navigation would invite lazy per-row loads in a list handler.
        builder.HasOne<AssemblyNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Codes are unique per business unit across all three levels â€” see
        // AssemblyNode.Code. Filtered on IsDeleted so a code belonging to a
        // soft-deleted node can be issued again.
        builder.HasIndex(n => new { n.BusinessUnitId, n.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_AssemblyNode_BusinessUnit_Code");

        // Two names that are the same under one parent are the ambiguity the legacy
        // duplicate check was reaching for; it compared name and level only, so the
        // same assembly name could not be reused under a different section even
        // when that was exactly right. Scoped to the parent here, and to the level
        // as well so two sections cannot share a name.
        builder.HasIndex(n => new { n.BusinessUnitId, n.Level, n.ParentId, n.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_AssemblyNode_BusinessUnit_Level_Parent_Name");

        // Covers the default grid ordering, which is per level.
        builder.HasIndex(n => new { n.BusinessUnitId, n.Level, n.Code })
            .HasDatabaseName("IX_AssemblyNode_BusinessUnit_Level_Code");

        // "What sits under this node?" â€” the child count on every grid row, and the
        // parent picker's filter.
        builder.HasIndex(n => new { n.BusinessUnitId, n.ParentId })
            .HasDatabaseName("IX_AssemblyNode_BusinessUnit_Parent");

        builder.Ignore(n => n.DomainEvents);
    }
}
