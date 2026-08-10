using Erp.Api.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Api.Persistence.Configurations;

internal sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LookupValue");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Type).HasMaxLength(50).IsRequired();

        builder.Property(l => l.Code).HasMaxLength(50).IsRequired();

        builder.Property(l => l.Name).HasMaxLength(150).IsRequired();

        // A list cannot offer the same code twice: two "NOS" options are
        // indistinguishable on screen and ambiguous in the data.
        builder.HasIndex(l => new { l.Type, l.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_LookupValue_Type_Code");

        // Covers the only query this table serves: the active options of a list,
        // in display order.
        builder.HasIndex(l => new { l.Type, l.IsActive, l.SortOrder })
            .HasDatabaseName("IX_LookupValue_Type_IsActive_SortOrder");

        builder.Ignore(l => l.DomainEvents);
    }
}
