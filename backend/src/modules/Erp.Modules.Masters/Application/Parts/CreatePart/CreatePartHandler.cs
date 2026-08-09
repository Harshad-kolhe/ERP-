using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.CreatePart;

internal sealed class CreatePartHandler(MastersDbContext db) : ICommandHandler<CreatePartCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreatePartCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var partNumber = command.PartNumber.Trim().ToUpperInvariant();

        // Checked here so the user gets a precise message rather than a database
        // error. The unique index is still what guarantees it — see the catch below.
        var alreadyExists = await db.Parts
            .AsNoTracking()
            .AnyAsync(p => p.PartNumber == partNumber, cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<Guid>(PartErrors.DuplicatePartNumber(partNumber));
        }

        var part = Part.Create(
            command.PartNumber,
            command.Description,
            command.CategoryId,
            command.UnitOfMeasureCode,
            command.HsnCode,
            command.DrawingNumber,
            PartAttributesMapping.ToDomain(command.Attributes));

        db.Parts.Add(part);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two requests passed the check above concurrently and the unique index
            // rejected the loser. The constraint is the source of truth, not the read.
            return Result.Failure<Guid>(PartErrors.DuplicatePartNumber(partNumber));
        }

        return Result.Success(part.Id.Value);
    }

    /// <summary>SQL Server 2601 (unique index) and 2627 (unique constraint).</summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
