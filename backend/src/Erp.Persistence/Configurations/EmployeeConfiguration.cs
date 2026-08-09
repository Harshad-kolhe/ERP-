using Erp.Persistence.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Employee");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.MiddleName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.Gender).HasMaxLength(20);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.State).HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.PhoneNo).HasMaxLength(30);
        builder.Property(e => e.UserName).HasMaxLength(100);

        // Length only — this column is not a credential store. See Employee.Password.
        builder.Property(e => e.Password).HasMaxLength(200);

        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.Designation).HasMaxLength(100);
        builder.Property(e => e.Qualification).HasMaxLength(200);
        builder.Property(e => e.BloodGroup).HasMaxLength(10);
        builder.Property(e => e.AadharNo).HasMaxLength(12);
        builder.Property(e => e.PanNo).HasMaxLength(10);
        builder.Property(e => e.PassportNo).HasMaxLength(20);
        builder.Property(e => e.UserEmpCode).HasMaxLength(50);

        // Primitive collections: EF stores these as JSON in a single column, so a
        // skill list costs no join table and no second query.
        builder.PrimitiveCollection(e => e.Skill).HasMaxLength(2000);
        builder.PrimitiveCollection(e => e.Strength).HasMaxLength(2000);

        // Payroll amounts. Explicit rather than relying on the context-wide money
        // default, so a later change to that default cannot silently reshape payroll.
        builder.Property(e => e.ProvidentFund).HasPrecision(18, 2);
        builder.Property(e => e.EmployeeStateInsurance).HasPrecision(18, 2);
        builder.Property(e => e.ProfessionalTax).HasPrecision(18, 2);
        builder.Property(e => e.IncomeTaxTds).HasPrecision(18, 2);
        builder.Property(e => e.GrossSalary).HasPrecision(18, 2);
        builder.Property(e => e.NetSalary).HasPrecision(18, 2);
        builder.Property(e => e.PerHourSalary).HasPrecision(18, 4);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(e => new { e.BusinessUnitId, e.EmployeeCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [EmployeeCode] IS NOT NULL")
            .HasDatabaseName("UX_Employee_BusinessUnit_EmployeeCode");

        // Covers the default grid ordering, which is by name.
        builder.HasIndex(e => new { e.BusinessUnitId, e.IsActive, e.FirstName, e.LastName })
            .HasDatabaseName("IX_Employee_BusinessUnit_IsActive_Name");

        builder.Ignore(e => e.DomainEvents);
    }
}
