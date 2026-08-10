using Erp.Api.Common.Excel;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Roles;
using Erp.Api.Features.Imports;
using Erp.Api.Persistence;
using Erp.Contracts.Import;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Roles;

public sealed class RolesImportService(ErpDbContext db)
{
    public const string TemplateContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string TemplateSheetName = "Roles";

    public const string TemplateFileName = $"{TemplateSheetName}-import-template{ImportLimits.FileExtension}";

    public static byte[] BuildTemplate() =>
        ExcelTemplateWriter.Build(TemplateSheetName, RoleImportColumns.All);

    public async Task<Result<ImportResultDto>> ImportAsync(
        ImportFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var sheet = ImportPipeline.OpenSheet(
            file.Content,
            file.FileName,
            file.Length,
            RoleImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("roles", rows.Count);
        var roles = new List<Role>(rows.Count);
        var names = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);
            var (role, name) = MapRow(reader);

            names.Add((row.Row, name));
            report.Add(reader.Errors);

            if (role is not null)
            {
                roles.Add(role);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, names, RoleImportColumns.RolesName.Header);

        await RejectNamesAlreadyInUse(report, names, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.MasterRoles.AddRange(roles);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (Role? Role, string? Name) MapRow(ImportRowReader reader)
    {
        var roleId = reader.WholeNumber(RoleImportColumns.RoleId);

        if (roleId is < 1)
        {
            reader.AddError("Role id must be a positive number.", RoleImportColumns.RoleId);
        }

        var name = reader.RequiredText(RoleImportColumns.RolesName);

        var role = new Role
        {
            RoleId = roleId ?? 0,
            RolesName = name,
            BypassBusinessUnit = reader.Boolean(RoleImportColumns.BypassBusinessUnit) ?? false,
            IsActive = reader.Boolean(RoleImportColumns.IsActive) ?? true,
        };

        return (reader.IsValid ? role : null, string.IsNullOrEmpty(name) ? null : name);
    }

    private async Task RejectNamesAlreadyInUse(
        ImportReport report,
        List<(int Row, string? Key)> names,
        CancellationToken cancellationToken)
    {
        var wanted = names
            .Select(entry => entry.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wanted.Count == 0)
        {
            return;
        }

        var taken = (await db.MasterRoles
                .AsNoTracking()
                .Where(role => role.RolesName != null && wanted.Contains(role.RolesName))
                .Select(role => role.RolesName!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (row, key) in names.Where(entry => entry.Key is not null && taken.Contains(entry.Key!)))
        {
            report.Add(row, $"A role named '{key}' already exists.", RoleImportColumns.RolesName.Header);
        }
    }
}
