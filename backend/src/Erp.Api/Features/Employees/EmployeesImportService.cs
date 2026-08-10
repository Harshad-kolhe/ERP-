using System.Globalization;
using Erp.Api.Common.Excel;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Employees;
using Erp.Api.Features.Imports;
using Erp.Api.Persistence;
using Erp.Contracts.Import;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Employees;

public sealed class EmployeesImportService(ErpDbContext db)
{
    public const string TemplateContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string TemplateSheetName = "Employees";

    public const string TemplateFileName = $"{TemplateSheetName}-import-template{ImportLimits.FileExtension}";

    public static byte[] BuildTemplate() =>
        ExcelTemplateWriter.Build(TemplateSheetName, EmployeeImportColumns.All);

    public async Task<Result<ImportResultDto>> ImportAsync(
        ImportFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var sheet = ImportPipeline.OpenSheet(
            file.Content,
            file.FileName,
            file.Length,
            EmployeeImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("employees", rows.Count);
        var employees = new List<Employee>(rows.Count);
        var keys = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);
            var (employee, key) = MapRow(reader);

            keys.Add((row.Row, key));
            report.Add(reader.Errors);

            if (employee is not null)
            {
                employees.Add(employee);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, keys, EmployeeImportColumns.EmployeeCode.Header);

        await RejectCodesAlreadyInUse(report, keys, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (Employee? Employee, string? Key) MapRow(ImportRowReader reader)
    {
        var code = reader.WholeNumber(EmployeeImportColumns.EmployeeCode);
        var key = code?.ToString(CultureInfo.InvariantCulture);

        if (code is < 1)
        {
            reader.AddError("Employee code must be a positive number.", EmployeeImportColumns.EmployeeCode);
        }

        var firstName = reader.RequiredText(EmployeeImportColumns.FirstName);
        var isActive = reader.Boolean(EmployeeImportColumns.IsActive) ?? true;
        var status = MasterStatusReader.Read(reader, EmployeeImportColumns.Status);

        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = firstName,
            MiddleName = reader.Text(EmployeeImportColumns.MiddleName),
            LastName = reader.Text(EmployeeImportColumns.LastName),
            Gender = ReadGender(reader),
            Address = reader.Text(EmployeeImportColumns.Address),
            State = reader.Text(EmployeeImportColumns.State),
            UserName = reader.Text(EmployeeImportColumns.UserName),
            RoleId = reader.WholeNumber(EmployeeImportColumns.RoleId),
            Department = reader.Text(EmployeeImportColumns.Department),
            Designation = reader.Text(EmployeeImportColumns.Designation),
            Email = reader.Text(EmployeeImportColumns.Email),
            PhoneNo = reader.Text(EmployeeImportColumns.PhoneNo),
            DateOfBirth = reader.Date(EmployeeImportColumns.DateOfBirth),
            JoiningDate = reader.Date(EmployeeImportColumns.JoiningDate),
            IsMarried = reader.Boolean(EmployeeImportColumns.IsMarried) ?? false,
            BloodGroup = reader.Text(EmployeeImportColumns.BloodGroup),
            ShoeSize = reader.WholeNumber(EmployeeImportColumns.ShoeSize),
            AadharNo = reader.Text(EmployeeImportColumns.AadharNo),
            PanNo = reader.Text(EmployeeImportColumns.PanNo),
            PassportNo = reader.Text(EmployeeImportColumns.PassportNo),
            Qualification = reader.Text(EmployeeImportColumns.Qualification),
            Skill = reader.TextList(EmployeeImportColumns.Skills),
            Strength = reader.TextList(EmployeeImportColumns.Strengths),
            IsOverTimeApplicable = reader.Boolean(EmployeeImportColumns.IsOverTimeApplicable),
            WillingToTravel = reader.Boolean(EmployeeImportColumns.WillingToTravel),
            ApplicableForService = reader.Boolean(EmployeeImportColumns.ApplicableForService),
            ProvidentFund = Money(reader, EmployeeImportColumns.ProvidentFund),
            EmployeeStateInsurance = Money(reader, EmployeeImportColumns.EmployeeStateInsurance),
            ProfessionalTax = Money(reader, EmployeeImportColumns.ProfessionalTax),
            IncomeTaxTds = Money(reader, EmployeeImportColumns.IncomeTaxTds),
            GrossSalary = Money(reader, EmployeeImportColumns.GrossSalary),
            NetSalary = Money(reader, EmployeeImportColumns.NetSalary),
            PerHourSalary = Money(reader, EmployeeImportColumns.PerHourSalary),
            IsActive = isActive,
            Status = status,
        };

        return (reader.IsValid ? employee : null, key);
    }

    private static string? ReadGender(ImportRowReader reader)
    {
        var text = reader.Text(EmployeeImportColumns.Gender);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Trim().ToLowerInvariant() switch
        {
            "01" or "m" or "male" => "Male",
            "02" or "f" or "female" => "Female",
            _ => text,
        };
    }

    private static decimal? Money(ImportRowReader reader, ImportColumn column)
    {
        var value = reader.Number(column);

        if (value is < 0)
        {
            reader.AddError("Cannot be negative.", column);
            return null;
        }

        return value;
    }

    private async Task RejectCodesAlreadyInUse(
        ImportReport report,
        List<(int Row, string? Key)> keys,
        CancellationToken cancellationToken)
    {
        var codes = keys
            .Select(entry => entry.Key)
            .Where(key => key is not null)
            .Select(key => int.Parse(key!, CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();

        if (codes.Count == 0)
        {
            return;
        }

        var taken = (await db.Employees
                .AsNoTracking()
                .Where(employee => employee.EmployeeCode != null && codes.Contains(employee.EmployeeCode.Value))
                .Select(employee => employee.EmployeeCode!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var (row, key) in keys.Where(entry => entry.Key is not null))
        {
            if (taken.Contains(int.Parse(key!, CultureInfo.InvariantCulture)))
            {
                report.Add(row, $"Employee code '{key}' already exists.", EmployeeImportColumns.EmployeeCode.Header);
            }
        }
    }
}
