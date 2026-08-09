using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.Persistence.Domain.Parts;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.UpdatePart;

internal sealed record UpdatePartCommand(
    Guid Id,
    string Description,
    Guid? CategoryId,
    string UnitOfMeasureCode,
    string? HsnCode,
    string? DrawingNumber,
    PartAttributesDto? Attributes,
    string RowVersion);

internal sealed class UpdatePartHandler(ErpDbContext db) : ICommandHandler<UpdatePartCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(UpdatePartCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!TryDecodeRowVersion(command.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(PartErrors.StaleRowVersion);
        }

        // Checked on update as well as create, not only on create. An edit can
        // introduce a code the masters have never heard of just as easily as a new
        // record can, and a rule enforced on one path is not a rule.
        var unknownCode = await PartCodedFields.FindUnknownAsync(
            db,
            new PartCodes(command.UnitOfMeasureCode, command.HsnCode, command.Attributes),
            cancellationToken);

        if (unknownCode is not null)
        {
            return Result.Failure<Unit>(unknownCode);
        }

        var partId = new PartId(command.Id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure<Unit>(PartErrors.NotFound(command.Id));
        }

        // Tell EF the version the client was looking at. If the row has moved on
        // since, the UPDATE matches zero rows and EF raises a concurrency exception
        // instead of silently discarding the other person's edit.
        db.Entry(part).Property(p => p.RowVersion).OriginalValue = rowVersion;

        var updated = part.Update(
            command.Description,
            command.CategoryId,
            command.UnitOfMeasureCode,
            command.HsnCode,
            command.DrawingNumber,
            PartAttributesMapping.ToDomain(command.Attributes));

        if (updated.IsFailure)
        {
            return Result.Failure<Unit>(updated.Error);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(PartErrors.StaleRowVersion);
        }

        return Result.Success(Unit.Value);
    }

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
