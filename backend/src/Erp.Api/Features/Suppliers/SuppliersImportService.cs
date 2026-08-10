using Erp.Api.Common.Excel;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Suppliers;
using Erp.Api.Features.Imports;
using Erp.Api.Persistence;
using Erp.Contracts.Import;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Suppliers;

public sealed class SuppliersImportService(ErpDbContext db)
{
    public const string TemplateContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string TemplateSheetName = "Suppliers";

    public const string TemplateFileName = $"{TemplateSheetName}-import-template{ImportLimits.FileExtension}";

    public static byte[] BuildTemplate() =>
        ExcelTemplateWriter.Build(TemplateSheetName, SupplierImportColumns.All);

    public async Task<Result<ImportResultDto>> ImportAsync(
        ImportFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var sheet = ImportPipeline.OpenSheet(
            file.Content,
            file.FileName,
            file.Length,
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
