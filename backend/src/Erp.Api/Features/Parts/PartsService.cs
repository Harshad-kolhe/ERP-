using Erp.Api.Common.Results;
using Erp.Api.Common.Security;
using Erp.Api.Common.Time;
using Erp.Api.Domain.Parts;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Parts;

public sealed class PartsService(ErpDbContext db, ICurrentUser currentUser, IClock clock)
{
    public async Task<Result<Guid>> CreateAsync(CreatePartRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var partNumber = request.PartNumber.Trim().ToUpperInvariant();

        var alreadyExists = await db.Parts
            .AsNoTracking()
            .AnyAsync(p => p.PartNumber == partNumber, cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<Guid>(PartErrors.DuplicatePartNumber(partNumber));
        }

        var unknownCode = await PartCodedFields.FindUnknownAsync(
            db,
            new PartCodes(request.UnitOfMeasureCode, request.HsnCode, request.Attributes),
            cancellationToken);

        if (unknownCode is not null)
        {
            return Result.Failure<Guid>(unknownCode);
        }

        var part = Part.Create(
            request.PartNumber,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasureCode,
            request.HsnCode,
            request.DrawingNumber,
            PartAttributesMapping.ToDomain(request.Attributes));

        db.Parts.Add(part);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result.Failure<Guid>(PartErrors.DuplicatePartNumber(partNumber));
        }

        return Result.Success(part.Id.Value);
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdatePartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(PartErrors.StaleRowVersion);
        }

        var unknownCode = await PartCodedFields.FindUnknownAsync(
            db,
            new PartCodes(request.UnitOfMeasureCode, request.HsnCode, request.Attributes),
            cancellationToken);

        if (unknownCode is not null)
        {
            return Result.Failure(unknownCode);
        }

        var partId = new PartId(id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure(PartErrors.NotFound(id));
        }

        db.Entry(part).Property(p => p.RowVersion).OriginalValue = rowVersion;

        var updated = part.Update(
            request.Description,
            request.CategoryId,
            request.UnitOfMeasureCode,
            request.HsnCode,
            request.DrawingNumber,
            PartAttributesMapping.ToDomain(request.Attributes));

        if (updated.IsFailure)
        {
            return Result.Failure(updated.Error);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(PartErrors.StaleRowVersion);
        }

        return Result.Success();
    }

    public async Task<Result> SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var partId = new PartId(id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure(PartErrors.NotFound(id));
        }

        var submitted = part.SubmitForApproval();

        if (submitted.IsFailure)
        {
            return Result.Failure(submitted.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var partId = new PartId(id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure(PartErrors.NotFound(id));
        }

        var approved = part.Approve(currentUser.UserId, clock.UtcNow);

        if (approved.IsFailure)
        {
            return Result.Failure(approved.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool TryDecodeRowVersion(string value, out byte[] rowVersion)
    {
        rowVersion = [];

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var buffer = new byte[((value.Length * 3) + 3) / 4];

        if (!Convert.TryFromBase64String(value, buffer, out var written))
        {
            return false;
        }

        rowVersion = buffer[..written];
        return true;
    }
}
