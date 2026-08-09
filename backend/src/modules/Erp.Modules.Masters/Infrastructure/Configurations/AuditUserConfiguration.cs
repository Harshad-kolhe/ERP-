using Erp.Modules.Masters.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Modules.Masters.Infrastructure.Configurations;

internal sealed class AuditUserConfiguration : IEntityTypeConfiguration<AuditUser>
{
    public void Configure(EntityTypeBuilder<AuditUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Keyless: nothing tracks, updates or deletes through this mapping, and EF
        // refusing to let it try is the point.
        builder.HasNoKey();

        // ToView rather than ToTable. EF excludes views from migrations, so this
        // module can read identity's table without ever claiming ownership of it —
        // no CREATE TABLE, no ALTER, nothing in the masters migration history.
        builder.ToView("AspNetUsers", "identity");

        builder.Property(u => u.Id).HasColumnName("Id");

        builder.Property(u => u.DisplayName).HasColumnName("DisplayName");
    }
}
