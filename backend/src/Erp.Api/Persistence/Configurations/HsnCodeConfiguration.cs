using Erp.Api.Domain.HsnCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Api.Persistence.Configurations;

internal sealed class HsnCodeConfiguration : IEntityTypeConfiguration<HsnCode>
{
    public void Configure(EntityTypeBuilder<HsnCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("HsnCode");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).ValueGeneratedOnAdd();

        builder.Property(h => h.Code).HasMaxLength(10).IsRequired();

        builder.Property(h => h.Description).HasMaxLength(250).IsRequired();

        builder.HasIndex(h => h.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_HsnCode_Code");

        // The rates belong to the code and are only ever reached through it, so they
        // are loaded through the backing field and deleted with it.
        builder.HasMany(h => h.Rates)
            .WithOne()
            .HasForeignKey(r => r.HsnCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(HsnCode.Rates))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(h => h.DomainEvents);
    }
}

internal sealed class HsnGstRateConfiguration : IEntityTypeConfiguration<HsnGstRate>
{
    public void Configure(EntityTypeBuilder<HsnGstRate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("HsnGstRate");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        // (5,2) â€” a percentage, not money. Enough for 100.00 and no more, so a rate
        // typed into the wrong column is rejected rather than stored.
        builder.Property(r => r.RatePercent).HasPrecision(5, 2).IsRequired();

        // One rate per code per date. Two rows claiming the same start date make
        // "which rate applied?" unanswerable, which is the one question this table exists to answer.
        builder.HasIndex(r => new { r.HsnCodeId, r.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("UX_HsnGstRate_HsnCode_EffectiveFrom");
    }
}
