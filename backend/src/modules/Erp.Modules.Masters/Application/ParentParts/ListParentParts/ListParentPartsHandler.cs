using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.ParentParts.ListParentParts;

internal sealed record ListParentPartsQuery(PageRequest Page);

/// <summary>
/// Returns one page of parent parts.
/// <para>
/// Headers only. The legacy grid listed headers and component lines from the same
/// table and told them apart by whether the child column was null, so the row count
/// on screen was neither the number of builds nor the number of lines.
/// </para>
/// <para>
/// The part number, the assembly code and the component count come from correlated
/// subqueries in the same statement — the legacy screen fetched the part master
/// separately and matched numbers up in the browser.
/// </para>
/// </summary>
internal sealed class ListParentPartsHandler(MastersDbContext db)
    : IQueryHandler<ListParentPartsQuery, PagedResult<ParentPartListItemDto>>
{
    /// <summary>
    /// The allow-list. <c>componentCount</c> is absent for the same reason the
    /// assembly grid's child count is: sorting on a subquery makes the database
    /// count every build's lines before it can order one page.
    /// </summary>
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
        .DefaultSort("partNumber")
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<ParentPartListItemDto>>> HandleAsync(
        ListParentPartsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

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
                CreatedBy = db.AuditUsers
                    .Where(user => user.Id == parentPart.CreatedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = parentPart.CreatedAtUtc,
                ModifiedBy = db.AuditUsers
                    .Where(user => user.Id == parentPart.ModifiedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = parentPart.ModifiedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);

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
