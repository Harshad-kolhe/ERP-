using Erp.Persistence.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Role");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.RolesName).HasMaxLength(100);

        // No business unit in this key: a role is cross-tenant by design — see Role.
        builder.HasIndex(r => r.RolesName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [RolesName] IS NOT NULL")
            .HasDatabaseName("UX_Role_RolesName");

        builder.HasIndex(r => r.RoleId)
            .HasDatabaseName("IX_Role_RoleId");

        builder.Ignore(r => r.DomainEvents);
    }
}
