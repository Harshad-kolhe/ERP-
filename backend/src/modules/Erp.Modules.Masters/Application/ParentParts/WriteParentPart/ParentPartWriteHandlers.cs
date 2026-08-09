using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.ParentParts.WriteParentPart;

internal sealed record GetParentPartByIdQuery(Guid Id);

internal sealed record CreateParentPartCommand(CreateParentPartRequest Request);

internal sealed record UpdateParentPartCommand(Guid Id, UpdateParentPartRequest Request);

/// <summary>
/// Returns one build and its lines, with every part number resolved server-side.
/// </summary>
internal sealed class GetParentPartByIdHandler(MastersDbContext db)
    : IQueryHandler<GetParentPartByIdQuery, ParentPartDetailDto>
{
    public async Task<Result<ParentPartDetailDto>> HandleAsync(
        GetParentPartByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = new ParentPartId(query.Id);

        var parentPart = await db.ParentParts
            .AsNoTracking()
            .Include(p => p.Components)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (parentPart is null)
        {
            return Result.Failure<ParentPartDetailDto>(ParentPartErrors.NotFound(query.Id));
        }

        // Every part on the record — the parent and each component — resolved in one
        // query. The alternative is a lookup per line, which is how the legacy
        // screen turned a twenty-line build into twenty-one requests.
        var partIds = parentPart.Components
            .Select(component => component.PartId)
            .Append(parentPart.PartId)
            .Distinct()
            .ToList();

        var parts = await db.Parts
            .AsNoTracking()
            .Where(part => partIds.Contains(part.Id))
            .Select(part => new { part.Id, part.PartNumber, part.Description, part.UnitOfMeasureCode })
            .ToDictionaryAsync(part => part.Id, cancellationToken);

        var assembly = parentPart.AssemblyNodeId is null
            ? null
            : await db.AssemblyNodes
                .AsNoTracking()
                .Where(node => node.Id == parentPart.AssemblyNodeId)
                .Select(node => new { node.Code, node.Name })
                .FirstOrDefaultAsync(cancellationToken);

        parts.TryGetValue(parentPart.PartId, out var parent);

        return Result.Success(new ParentPartDetailDto
        {
            Id = parentPart.Id.Value,
            PartId = parentPart.PartId.Value,
            PartNumber = parent?.PartNumber ?? string.Empty,
            PartDescription = parent?.Description ?? string.Empty,
            Description = parentPart.Description,
            AssemblyNodeId = parentPart.AssemblyNodeId?.Value,
            AssemblyCode = assembly?.Code,
            AssemblyName = assembly?.Name,
            UnitOfMeasureCode = parentPart.UnitOfMeasureCode,
            DrawingNumber = parentPart.DrawingNumber,
            Category = parentPart.Category,
            Components =
            [
                .. parentPart.Components
                    .OrderBy(component => component.LineNumber)
                    .Select(component =>
                    {
                        parts.TryGetValue(component.PartId, out var part);

                        return new ParentPartComponentDto
                        {
                            PartId = component.PartId.Value,
                            PartNumber = part?.PartNumber,
                            PartDescription = part?.Description,
                            Quantity = component.Quantity,

                            // Falls back to the part's own unit rather than blank:
                            // a line whose unit is unstated is measured in whatever
                            // the part master says, and showing nothing there is how
                            // a quantity gets read as the wrong unit.
                            UnitOfMeasureCode = component.UnitOfMeasureCode ?? part?.UnitOfMeasureCode,
                            UnitWeightKg = component.UnitWeightKg,
                            Rate = component.Rate,
                            Amount = component.Amount,
                            LineWeightKg = component.LineWeightKg,
                            DrawingNumber = component.DrawingNumber,
                            Remark = component.Remark,
                        };
                    }),
            ],
            TotalWeightKg = parentPart.TotalWeightKg,
            TotalAmount = parentPart.TotalAmount,
            IsActive = parentPart.IsActive,
            BusinessUnitId = parentPart.BusinessUnitId,
            CreatedAtUtc = parentPart.CreatedAtUtc,
            ModifiedAtUtc = parentPart.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(parentPart.RowVersion),
        });
    }
}

internal sealed class CreateParentPartHandler(MastersDbContext db)
    : ICommandHandler<CreateParentPartCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateParentPartCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = command.Request;
        var partId = new PartId(request.PartId);

        var partNumber = await db.Parts
            .AsNoTracking()
            .Where(part => part.Id == partId)
            .Select(part => part.PartNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (partNumber is null)
        {
            return Result.Failure<Guid>(ParentPartErrors.PartNotFound(request.PartId));
        }

        // Checked so the user is sent to the existing record rather than shown a
        // database error. The unique index is still what guarantees it.
        if (await db.ParentParts.AsNoTracking().AnyAsync(p => p.PartId == partId, cancellationToken))
        {
            return Result.Failure<Guid>(ParentPartErrors.AlreadyDefined(partNumber));
        }

        var assemblyNode = await ParentPartComposition.ResolveAssemblyNodeAsync(
            db,
            request.AssemblyNodeId,
            cancellationToken);

        if (assemblyNode.IsFailure)
        {
            return Result.Failure<Guid>(assemblyNode.Error);
        }

        var components = await ParentPartComposition.BuildAsync(
            db,
            partId,
            request.Components,
            cancellationToken);

        if (components.IsFailure)
        {
            return Result.Failure<Guid>(components.Error);
        }

        var parentPart = ParentPart.Create(
            partId,
            assemblyNode.Value,
            request.Description,
            request.UnitOfMeasureCode,
            request.DrawingNumber,
            request.Category,
            components.Value);

        db.ParentParts.Add(parentPart);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Two requests passed the check above concurrently and the index
            // rejected the loser. The constraint is the source of truth.
            return Result.Failure<Guid>(ParentPartErrors.AlreadyDefined(partNumber));
        }

        return Result.Success(parentPart.Id.Value);
    }

    /// <summary>SQL Server 2601 (unique index) and 2627 (unique constraint).</summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateParentPartHandler(MastersDbContext db)
    : ICommandHandler<UpdateParentPartCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateParentPartCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = command.Request;

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(ParentPartErrors.StaleRowVersion);
        }

        var id = new ParentPartId(command.Id);

        // Tracked, and with the lines loaded: replacing the collection needs EF to
        // know which rows were there before so it can delete the ones that are gone.
        var parentPart = await db.ParentParts
            .Include(p => p.Components)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (parentPart is null)
        {
            return Result.Failure<Unit>(ParentPartErrors.NotFound(command.Id));
        }

        var assemblyNode = await ParentPartComposition.ResolveAssemblyNodeAsync(
            db,
            request.AssemblyNodeId,
            cancellationToken);

        if (assemblyNode.IsFailure)
        {
            return Result.Failure<Unit>(assemblyNode.Error);
        }

        var components = await ParentPartComposition.BuildAsync(
            db,
            parentPart.PartId,
            request.Components,
            cancellationToken);

        if (components.IsFailure)
        {
            return Result.Failure<Unit>(components.Error);
        }

        // Tell EF the version the client was looking at. If the row has moved on,
        // the UPDATE matches zero rows and EF raises rather than silently discarding
        // the other person's edit — including their component lines.
        db.Entry(parentPart).Property(p => p.RowVersion).OriginalValue = rowVersion;

        parentPart.Update(
            assemblyNode.Value,
            request.Description,
            request.UnitOfMeasureCode,
            request.DrawingNumber,
            request.Category,
            request.IsActive,
            components.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(ParentPartErrors.StaleRowVersion);
        }

        return Result.Success(Unit.Value);
    }
}
