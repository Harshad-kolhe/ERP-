using Erp.Api.Common.Results;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HsnCodeEntity = Erp.Api.Domain.HsnCodes.HsnCode;

namespace Erp.Api.Features.HsnCodes;

public static class HsnCodeMapping
{
    public static HsnCodeDetailDto ToDetail(HsnCodeEntity hsn)
    {
        ArgumentNullException.ThrowIfNull(hsn);

        return new HsnCodeDetailDto
        {
            Id = hsn.Id,
            Code = hsn.Code,
            Description = hsn.Description,
            IsActive = hsn.IsActive,
            Rates =
            [
                .. hsn.Rates
                    .OrderByDescending(rate => rate.EffectiveFrom)
                    .Select(rate => new HsnGstRateDto
                    {
                        RatePercent = rate.RatePercent,
                        EffectiveFrom = rate.EffectiveFrom,
                    }),
            ],
            CreatedAtUtc = hsn.CreatedAtUtc,
            ModifiedAtUtc = hsn.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(hsn.RowVersion),
        };
    }
}

public sealed class HsnCodesService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateHsnCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = request.Code.Trim();

        var exists = await db.HsnCodes
            .AsNoTracking()
            .AnyAsync(hsn => hsn.Code == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("HSN code", "code", code));
        }

        var hsn = new HsnCodeEntity
        {
            Code = code,
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
        };

        hsn.AddRate(request.RatePercent, request.EffectiveFrom);

        db.HsnCodes.Add(hsn);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("HSN code", "code", code));
        }

        return Result.Success(hsn.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateHsnCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("HSN code"));
        }

        var hsn = await db.HsnCodes.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hsn is null)
        {
            return Result.Failure(MasterErrors.NotFound("HSN code", id));
        }

        db.Entry(hsn).Property(h => h.RowVersion).OriginalValue = rowVersion;

        hsn.Description = request.Description.Trim();
        hsn.IsActive = request.IsActive;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("HSN code"));
        }

        return Result.Success();
    }

    public async Task<Result> AddRateAsync(
        int id,
        AddHsnGstRateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hsn = await db.HsnCodes
            .Include(h => h.Rates)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hsn is null)
        {
            return Result.Failure(MasterErrors.NotFound("HSN code", id));
        }

        if (hsn.Rates.Any(rate => rate.EffectiveFrom == request.EffectiveFrom))
        {
            return Result.Failure(Error.Conflict(
                "hsn.rate.duplicate",
                $"A rate already applies from {request.EffectiveFrom:yyyy-MM-dd}. "
                + "Supersede it from a later date instead."));
        }

        hsn.AddRate(request.RatePercent, request.EffectiveFrom);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure(Error.Conflict(
                "hsn.rate.duplicate",
                $"A rate already applies from {request.EffectiveFrom:yyyy-MM-dd}."));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
