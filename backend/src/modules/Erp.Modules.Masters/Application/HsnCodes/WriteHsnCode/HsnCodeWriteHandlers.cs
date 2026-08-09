using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HsnCodeEntity = Erp.Persistence.Domain.HsnCodes.HsnCode;

namespace Erp.Modules.Masters.Application.HsnCodes.WriteHsnCode;

internal sealed record GetHsnCodeByIdQuery(int Id);

internal sealed record CreateHsnCodeCommand(CreateHsnCodeRequest Request);

internal sealed record UpdateHsnCodeCommand(int Id, UpdateHsnCodeRequest Request);

internal sealed record AddHsnGstRateCommand(int Id, AddHsnGstRateRequest Request);

internal static class HsnCodeMapping
{
    public static HsnCodeDetailDto ToDetail(HsnCodeEntity hsn) => new()
    {
        Id = hsn.Id,
        Code = hsn.Code,
        Description = hsn.Description,
        IsActive = hsn.IsActive,

        // Newest first: the rate somebody is about to supersede is the one they came
        // to look at, and the history below it is context.
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

internal sealed class GetHsnCodeByIdHandler(ErpDbContext db)
    : IQueryHandler<GetHsnCodeByIdQuery, HsnCodeDetailDto>
{
    public async Task<Result<HsnCodeDetailDto>> HandleAsync(
        GetHsnCodeByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var hsn = await db.HsnCodes
            .AsNoTracking()
            .Include(h => h.Rates)
            .FirstOrDefaultAsync(h => h.Id == query.Id, cancellationToken);

        return hsn is null
            ? Result.Failure<HsnCodeDetailDto>(MasterErrors.NotFound("HSN code", query.Id))
            : Result.Success(HsnCodeMapping.ToDetail(hsn));
    }
}

internal sealed class CreateHsnCodeHandler(ErpDbContext db)
    : ICommandHandler<CreateHsnCodeCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateHsnCodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = command.Request.Code.Trim();

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
            Description = command.Request.Description.Trim(),
            IsActive = command.Request.IsActive,
        };

        // Created with its opening rate in one transaction. A code that briefly
        // exists with no rate is a code a part could be saved against and then
        // invoiced at nothing.
        hsn.AddRate(command.Request.RatePercent, command.Request.EffectiveFrom);

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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateHsnCodeHandler(ErpDbContext db)
    : ICommandHandler<UpdateHsnCodeCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateHsnCodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("HSN code"));
        }

        var hsn = await db.HsnCodes.FirstOrDefaultAsync(h => h.Id == command.Id, cancellationToken);

        if (hsn is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("HSN code", command.Id));
        }

        db.Entry(hsn).Property(h => h.RowVersion).OriginalValue = rowVersion;

        // Description and active flag only. The code is what parts store, and the
        // rates are added rather than edited — see AddHsnGstRateHandler.
        hsn.Description = command.Request.Description.Trim();
        hsn.IsActive = command.Request.IsActive;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("HSN code"));
        }

        return Result.Success(Unit.Value);
    }
}

/// <summary>
/// Records a rate change.
/// <para>
/// Add-only, and that is the design rather than a missing feature. The rates are a
/// table so that an invoice raised last March still prices at last March's rate; an
/// endpoint that edited an existing row would rewrite the tax on every document
/// that reads it. A rate entered wrongly is corrected by superseding it from the
/// date the correct one applies.
/// </para>
/// </summary>
internal sealed class AddHsnGstRateHandler(ErpDbContext db)
    : ICommandHandler<AddHsnGstRateCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        AddHsnGstRateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var hsn = await db.HsnCodes
            .Include(h => h.Rates)
            .FirstOrDefaultAsync(h => h.Id == command.Id, cancellationToken);

        if (hsn is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("HSN code", command.Id));
        }

        // Checked here so the message names the date rather than surfacing a
        // constraint violation. The unique index is still what guarantees it.
        if (hsn.Rates.Any(rate => rate.EffectiveFrom == command.Request.EffectiveFrom))
        {
            return Result.Failure<Unit>(Error.Conflict(
                "hsn.rate.duplicate",
                $"A rate already applies from {command.Request.EffectiveFrom:yyyy-MM-dd}. "
                + "Supersede it from a later date instead."));
        }

        hsn.AddRate(command.Request.RatePercent, command.Request.EffectiveFrom);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure<Unit>(Error.Conflict(
                "hsn.rate.duplicate",
                $"A rate already applies from {command.Request.EffectiveFrom:yyyy-MM-dd}."));
        }

        return Result.Success(Unit.Value);
    }
}
