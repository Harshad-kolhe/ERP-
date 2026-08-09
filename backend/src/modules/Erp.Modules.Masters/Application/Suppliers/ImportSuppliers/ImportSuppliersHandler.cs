using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Excel;
using Erp.Contracts.Import;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Domain.Common;
using Erp.Modules.Masters.Domain.Suppliers;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Suppliers.ImportSuppliers;

internal sealed record ImportSuppliersCommand(ImportFile File);

/// <summary>Loads a sheet of suppliers. Same shape as <c>ImportPartsHandler</c> — see it for the reasoning.</summary>
internal sealed class ImportSuppliersHandler(MastersDbContext db)
    : ICommandHandler<ImportSuppliersCommand, ImportResultDto>
{
    public async Task<Result<ImportResultDto>> HandleAsync(
        ImportSuppliersCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = ImportPipeline.OpenSheet(
            command.File.Content,
            command.File.FileName,
            command.File.Length,
            SupplierImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("suppliers", rows.Count);
        var suppliers = new List<Supplier>(rows.Count);
        var keys = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);
            var (supplier, key) = MapRow(reader);

            keys.Add((row.Row, key));
            report.Add(reader.Errors);

            if (supplier is not null)
            {
                suppliers.Add(supplier);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, keys, SupplierImportColumns.SupplierCode.Header);

        await RejectCodesAlreadyInUse(report, keys, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.Suppliers.AddRange(suppliers);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (Supplier? Supplier, string? Key) MapRow(ImportRowReader reader)
    {
        var code = reader.RequiredText(SupplierImportColumns.SupplierCode);
        var key = string.IsNullOrEmpty(code) ? null : code.ToUpperInvariant();

        var name = reader.RequiredText(SupplierImportColumns.SupplierName);
        var isActive = reader.Boolean(SupplierImportColumns.IsActive) ?? true;
        var status = MasterStatusReader.Read(reader, SupplierImportColumns.Status);

        var supplier = new Supplier
        {
            // Upper-cased for the same reason part numbers are: 'acme' and 'ACME'
            // must not become two suppliers.
            SupplierCode = key,
            SupplierName = name,
            SupplierType = reader.Text(SupplierImportColumns.SupplierType),
            PrimaryContact = reader.Text(SupplierImportColumns.PrimaryContact),
            SecondaryContact = reader.Text(SupplierImportColumns.SecondaryContact),
            Phone = reader.Text(SupplierImportColumns.Phone),
            AltPhone = reader.Text(SupplierImportColumns.AltPhone),
            Email = reader.Text(SupplierImportColumns.Email),
            AltEmail = reader.Text(SupplierImportColumns.AltEmail),
            Website = reader.Text(SupplierImportColumns.Website),
            BillingAddress = reader.Text(SupplierImportColumns.BillingAddress),
            BillingCountry = reader.Text(SupplierImportColumns.BillingCountry),
            BillingState = reader.Text(SupplierImportColumns.BillingState),
            BillingCity = reader.Text(SupplierImportColumns.BillingCity),
            BillingZipCode = reader.Text(SupplierImportColumns.BillingZipCode),
            ShippingAddress = reader.Text(SupplierImportColumns.ShippingAddress),
            ShippingCountry = reader.Text(SupplierImportColumns.ShippingCountry),
            ShippingState = reader.Text(SupplierImportColumns.ShippingState),
            ShippingCity = reader.Text(SupplierImportColumns.ShippingCity),
            ShippingZipCode = reader.Text(SupplierImportColumns.ShippingZipCode),
            Pan = reader.Text(SupplierImportColumns.Pan),
            TaxId = reader.Text(SupplierImportColumns.TaxId),
            GstNo = reader.Text(SupplierImportColumns.GstNo),
            BankName = reader.Text(SupplierImportColumns.BankName),
            AccountNumber = reader.Text(SupplierImportColumns.AccountNumber),
            Ifsc = reader.Text(SupplierImportColumns.Ifsc),
            Swift = reader.Text(SupplierImportColumns.Swift),
            PaymentTerms = reader.Text(SupplierImportColumns.PaymentTerms),
            Currency = reader.Text(SupplierImportColumns.Currency)?.ToUpperInvariant(),
            TaxCode = reader.Text(SupplierImportColumns.TaxCode),
            QualityCompliance = reader.Text(SupplierImportColumns.QualityCompliance),
            Igst = TaxRate.Read(reader, SupplierImportColumns.Igst),
            Cgst = TaxRate.Read(reader, SupplierImportColumns.Cgst),
            Sgst = TaxRate.Read(reader, SupplierImportColumns.Sgst),
            ActiveStatus = reader.Text(SupplierImportColumns.ActiveStatus),
            IsActive = isActive,
            Status = status,
            ProgramId = ImportProvenance.ProgramId,
        };

        return (reader.IsValid ? supplier : null, key);
    }

    /// <summary>One query for the file. See <c>ImportPartsHandler</c> for why the index remains the guarantee.</summary>
    private async Task RejectCodesAlreadyInUse(
        ImportReport report,
        List<(int Row, string? Key)> keys,
        CancellationToken cancellationToken)
    {
        var codes = keys
            .Select(entry => entry.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (codes.Count == 0)
        {
            return;
        }

        var taken = (await db.Suppliers
                .AsNoTracking()
                .Where(supplier => codes.Contains(supplier.SupplierCode!))
                .Select(supplier => supplier.SupplierCode!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (row, key) in keys.Where(entry => entry.Key is not null && taken.Contains(entry.Key)))
        {
            report.Add(row, $"Supplier '{key}' already exists.", SupplierImportColumns.SupplierCode.Header);
        }
    }
}
