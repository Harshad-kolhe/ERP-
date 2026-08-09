using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Excel;
using Erp.Contracts.Import;
using Erp.Modules.Masters.Application.Imports;
using Erp.Persistence;
using Erp.Persistence.Domain.Customers;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Customers.ImportCustomers;

internal sealed record ImportCustomersCommand(ImportFile File);

/// <summary>Loads a sheet of customers. Same shape as <c>ImportPartsHandler</c> — see it for the reasoning.</summary>
internal sealed class ImportCustomersHandler(ErpDbContext db)
    : ICommandHandler<ImportCustomersCommand, ImportResultDto>
{
    public async Task<Result<ImportResultDto>> HandleAsync(
        ImportCustomersCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = ImportPipeline.OpenSheet(
            command.File.Content,
            command.File.FileName,
            command.File.Length,
            CustomerImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("customers", rows.Count);
        var customers = new List<Customer>(rows.Count);
        var keys = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);
            var (customer, key) = MapRow(reader);

            keys.Add((row.Row, key));
            report.Add(reader.Errors);

            if (customer is not null)
            {
                customers.Add(customer);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, keys, CustomerImportColumns.CustomerCode.Header);

        await RejectCodesAlreadyInUse(report, keys, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (Customer? Customer, string? Key) MapRow(ImportRowReader reader)
    {
        var code = reader.RequiredText(CustomerImportColumns.CustomerCode);
        var key = string.IsNullOrEmpty(code) ? null : code.ToUpperInvariant();

        var name = reader.RequiredText(CustomerImportColumns.CustomerName);
        var isActive = reader.Boolean(CustomerImportColumns.IsActive) ?? true;
        var status = MasterStatusReader.Read(reader, CustomerImportColumns.Status);

        var customer = new Customer
        {
            CustomerCode = key,
            CustomerName = name,
            Industry = reader.Text(CustomerImportColumns.Industry),
            PrimaryContact = reader.Text(CustomerImportColumns.PrimaryContact),
            SecondaryContact = reader.Text(CustomerImportColumns.SecondaryContact),
            Phone = reader.Text(CustomerImportColumns.Phone),
            AltPhone = reader.Text(CustomerImportColumns.AltPhone),
            Email = reader.Text(CustomerImportColumns.Email),
            AltEmail = reader.Text(CustomerImportColumns.AltEmail),
            Website = reader.Text(CustomerImportColumns.Website),
            BillingAddress = reader.Text(CustomerImportColumns.BillingAddress),
            BillingCountry = reader.Text(CustomerImportColumns.BillingCountry),
            BillingState = reader.Text(CustomerImportColumns.BillingState),
            BillingCity = reader.Text(CustomerImportColumns.BillingCity),
            BillingZipCode = reader.Text(CustomerImportColumns.BillingZipCode),
            ShippingAddress = reader.Text(CustomerImportColumns.ShippingAddress),
            ShippingCountry = reader.Text(CustomerImportColumns.ShippingCountry),
            ShippingState = reader.Text(CustomerImportColumns.ShippingState),
            ShippingCity = reader.Text(CustomerImportColumns.ShippingCity),
            ShippingZipCode = reader.Text(CustomerImportColumns.ShippingZipCode),
            TaxId = reader.Text(CustomerImportColumns.TaxId),
            Gst = reader.Text(CustomerImportColumns.Gst),
            Pan = reader.Text(CustomerImportColumns.Pan),
            Igst = TaxRate.Read(reader, CustomerImportColumns.Igst),
            Cgst = TaxRate.Read(reader, CustomerImportColumns.Cgst),
            Sgst = TaxRate.Read(reader, CustomerImportColumns.Sgst),
            Currency = reader.Text(CustomerImportColumns.Currency)?.ToUpperInvariant(),
            TaxCode = reader.Text(CustomerImportColumns.TaxCode),
            IsActive = isActive,
            Status = status,
        };

        return (reader.IsValid ? customer : null, key);
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

        var taken = (await db.Customers
                .AsNoTracking()
                .Where(customer => codes.Contains(customer.CustomerCode!))
                .Select(customer => customer.CustomerCode!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (row, key) in keys.Where(entry => entry.Key is not null && taken.Contains(entry.Key)))
        {
            report.Add(row, $"Customer '{key}' already exists.", CustomerImportColumns.CustomerCode.Header);
        }
    }
}
