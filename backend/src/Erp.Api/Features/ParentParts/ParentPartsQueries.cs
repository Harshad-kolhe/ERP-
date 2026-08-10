using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Assemblies;
using Erp.Api.Domain.ParentParts;
using Erp.Api.Domain.Parts;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.ParentParts;

public sealed record ParentPartListRow
{
    public required ParentPartId Id { get; init; }

    public required PartId PartId { get; init; }

    public required string PartNumber { get; init; }

    public required string PartDescription { get; init; }

    public required string? Description { get; init; }

    public required AssemblyNodeId? AssemblyNodeId { get; init; }

    public required string? AssemblyCode { get; init; }

    public required string? AssemblyName { get; init; }

    public required string? UnitOfMeasureCode { get; init; }

    public required string? DrawingNumber { get; init; }

    public required string? Category { get; init; }

    public required int ComponentCount { get; init; }

    public required decimal TotalWeightKg { get; init; }

    public required decimal TotalAmount { get; init; }

    public required bool IsActive { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}

public sealed class ParentPartsQueries(ErpDbContext db)
{
    private static readonly QueryMap<ParentPartListRow> Map = QueryMap<ParentPartListRow>.Create()
        .Field("partNumber", x => x.PartNumber, searchable: true)
        .Field("partDescription", x => x.PartDescription, searchable: true)
        .Field("description", x => x.Description, searchable: true)
        .Field("assemblyCode", x => x.AssemblyCode, searchable: true)
        .Field("assemblyName", x => x.AssemblyName)
        .Field("unitOfMeasureCode", x => x.UnitOfMeasureCode)
        .Field("drawingNumber", x => x.DrawingNumber)
        .Field("category", x => x.Category)
        .Field("totalWeightKg", x => x.TotalWeightKg)
        .Field("totalAmount", x => x.TotalAmount)
        .Field("isActive", x => x.IsActive)
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<ParentPartListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.ParentParts
            .AsNoTracking()
            .Select(parentPart => new ParentPartListRow
            {
                Id = parentPart.Id,
                PartId = parentPart.PartId,
                PartNumber = db.Parts
                    .Where(part => part.Id == parentPart.PartId)
                    .Select(part => part.PartNumber)
                    .FirstOrDefault()!,
                PartDescription = db.Parts
                    .Where(part => part.Id == parentPart.PartId)
                    .Select(part => part.Description)
                    .FirstOrDefault()!,
                Description = parentPart.Description,
                AssemblyNodeId = parentPart.AssemblyNodeId,
                AssemblyCode = db.AssemblyNodes
                    .Where(node => node.Id == parentPart.AssemblyNodeId)
                    .Select(node => node.Code)
                    .FirstOrDefault(),
                AssemblyName = db.AssemblyNodes
                    .Where(node => node.Id == parentPart.AssemblyNodeId)
                    .Select(node => node.Name)
                    .FirstOrDefault(),
                UnitOfMeasureCode = parentPart.UnitOfMeasureCode,
                DrawingNumber = parentPart.DrawingNumber,
                Category = parentPart.Category,
                ComponentCount = parentPart.Components.Count,
                TotalWeightKg = parentPart.TotalWeightKg,
                TotalAmount = parentPart.TotalAmount,
                IsActive = parentPart.IsActive,
                CreatedBy = db.Users
                    .Where(user => user.Id == parentPart.CreatedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = parentPart.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(user => user.Id == parentPart.ModifiedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = parentPart.ModifiedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, request, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<ParentPartListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<ParentPartListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    public async Task<Result<ParentPartDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var parentPartId = new ParentPartId(id);

        var parentPart = await db.ParentParts
            .AsNoTracking()
            .Include(p => p.Components)
            .FirstOrDefaultAsync(p => p.Id == parentPartId, cancellationToken);

        if (parentPart is null)
        {
            return Result.Failure<ParentPartDetailDto>(ParentPartErrors.NotFound(id));
        }

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

    private static ParentPartListItemDto ToDto(ParentPartListRow row) => new()
    {
        Id = row.Id.Value,
        PartId = row.PartId.Value,
        PartNumber = row.PartNumber,
        PartDescription = row.PartDescription,
        Description = row.Description,
        AssemblyNodeId = row.AssemblyNodeId?.Value,
        AssemblyCode = row.AssemblyCode,
        AssemblyName = row.AssemblyName,
        UnitOfMeasureCode = row.UnitOfMeasureCode,
        DrawingNumber = row.DrawingNumber,
        Category = row.Category,
        ComponentCount = row.ComponentCount,
        TotalWeightKg = row.TotalWeightKg,
        TotalAmount = row.TotalAmount,
        IsActive = row.IsActive,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
