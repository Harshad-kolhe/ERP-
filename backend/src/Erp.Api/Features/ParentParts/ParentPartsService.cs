using Erp.Api.Common.Results;
using Erp.Api.Domain.ParentParts;
using Erp.Api.Domain.Parts;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.ParentParts;

public sealed class ParentPartsService(ErpDbContext db)
{
    public async Task<Result<Guid>> CreateAsync(
        CreateParentPartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            return Result.Failure<Guid>(ParentPartErrors.AlreadyDefined(partNumber));
        }

        return Result.Success(parentPart.Id.Value);
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdateParentPartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(ParentPartErrors.StaleRowVersion);
        }

        var parentPartId = new ParentPartId(id);

        var parentPart = await db.ParentParts
            .Include(p => p.Components)
            .FirstOrDefaultAsync(p => p.Id == parentPartId, cancellationToken);

        if (parentPart is null)
        {
            return Result.Failure(ParentPartErrors.NotFound(id));
        }

        var assemblyNode = await ParentPartComposition.ResolveAssemblyNodeAsync(
            db,
            request.AssemblyNodeId,
            cancellationToken);

        if (assemblyNode.IsFailure)
        {
            return Result.Failure(assemblyNode.Error);
        }

        var components = await ParentPartComposition.BuildAsync(
            db,
            parentPart.PartId,
            request.Components,
            cancellationToken);

        if (components.IsFailure)
        {
            return Result.Failure(components.Error);
        }

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
            return Result.Failure(ParentPartErrors.StaleRowVersion);
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
