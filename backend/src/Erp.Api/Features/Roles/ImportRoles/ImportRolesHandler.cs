using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Excel;
using Erp.Contracts.Import;
using Erp.Api.Features.Imports;
using Erp.Api.Persistence;
using Erp.Api.Domain.Roles;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Roles.ImportRoles;

public sealed record ImportRolesCommand(ImportFile File);

/// <summary>
/// Loads a sheet of legacy role master rows.
/// <para>
/// Worth being clear about what this does not do: it grants nothing. These rows
/// exist so <c>Employee.RoleId</c> has something to point at. Permissions live on
/// Identity roles and are edited on the roles administration screen, so no
/// spreadsheet can hand anybody a permission.
/// </para>
/// <para>
/// The name is unique system-wide rather than per business unit, because a role is
/// a cross-tenant concept here â€” see <c>Role</c>.
/// </para>
/// </summary>
public sealed class ImportRolesHandler(ErpDbContext db)
    : ICommandHandler<ImportRolesCommand, ImportResultDto>
{
    public async Task<Result<ImportResultDto>> HandleAsync(
        ImportRolesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = ImportPipeline.OpenSheet(
            command.File.Content,
            command.File.FileName,
            command.File.Length,
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
