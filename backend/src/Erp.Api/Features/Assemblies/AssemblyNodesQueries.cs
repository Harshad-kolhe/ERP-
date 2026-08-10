using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Assemblies;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Assemblies;

public sealed record AssemblyNodeListRow
{
    public required AssemblyNodeId Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string? ManualCode { get; init; }

    public required AssemblyLevel Level { get; init; }

    public required AssemblyNodeId? ParentId { get; init; }

    public required string? ParentCode { get; init; }

    public required string? ParentName { get; init; }

    public required int ChildCount { get; init; }

    public required string? MachineType { get; init; }

    public required string? DrivenBy { get; init; }

    public required string? DrawingPath { get; init; }

    public required string? TechnicalSpecification { get; init; }

    public required string? Remark { get; init; }

    public required decimal? Quantity { get; init; }

    public required decimal? WeightKg { get; init; }

    public required int? DisplaySequence { get; init; }

    public required bool IsActive { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}

public sealed class AssemblyNodesQueries(ErpDbContext db)
{
    private static readonly QueryMap<AssemblyNodeListRow> Map = QueryMap<AssemblyNodeListRow>.Create()
        .Field("code", x => x.Code, searchable: true)
        .Field("name", x => x.Name, searchable: true)
        .Field("manualCode", x => x.ManualCode, searchable: true)
        .Field("parentCode", x => x.ParentCode, searchable: true)
        .Field("parentName", x => x.ParentName)
        .Field("machineType", x => x.MachineType)
        .Field("drivenBy", x => x.DrivenBy)
        .Field("drawingPath", x => x.DrawingPath)
        .Field("technicalSpecification", x => x.TechnicalSpecification)
        .Field("remark", x => x.Remark)
        .Field("quantity", x => x.Quantity)
        .Field("weightKg", x => x.WeightKg)
        .Field("displaySequence", x => x.DisplaySequence)
        .Field("isActive", x => x.IsActive)
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<AssemblyNodeListItemDto>>> ListAsync(
        AssemblyLevel level,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.AssemblyNodes
            .AsNoTracking()
            .Where(node => node.Level == level)
            .Select(node => new AssemblyNodeListRow
            {
                Id = node.Id,
                Code = node.Code,
                Name = node.Name,
                ManualCode = node.ManualCode,
                Level = node.Level,
                ParentId = node.ParentId,
                ParentCode = db.AssemblyNodes
                    .Where(parent => parent.Id == node.ParentId)
                    .Select(parent => parent.Code)
                    .FirstOrDefault(),
                ParentName = db.AssemblyNodes
                    .Where(parent => parent.Id == node.ParentId)
                    .Select(parent => parent.Name)
                    .FirstOrDefault(),
                ChildCount = db.AssemblyNodes.Count(child => child.ParentId == node.Id),
                MachineType = node.MachineType,
                DrivenBy = node.DrivenBy,
                DrawingPath = node.DrawingPath,
                TechnicalSpecification = node.TechnicalSpecification,
                Remark = node.Remark,
                Quantity = node.Quantity,
                WeightKg = node.WeightKg,
                DisplaySequence = node.DisplaySequence,
                IsActive = node.IsActive,
                CreatedBy = db.Users
                    .Where(user => user.Id == node.CreatedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = node.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(user => user.Id == node.ModifiedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = node.ModifiedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, request, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<AssemblyNodeListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<AssemblyNodeListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    public async Task<Result<AssemblyNodeDetailDto>> GetByIdAsync(
        AssemblyLevel level,
        Guid id,
        CancellationToken cancellationToken)
    {
        var nodeId = new AssemblyNodeId(id);

        var node = await db.AssemblyNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.Level == level, cancellationToken);

        if (node is null)
        {
            return Result.Failure<AssemblyNodeDetailDto>(AssemblyErrors.NotFound(level, id));
        }

        var parent = node.ParentId is null
            ? null
            : await db.AssemblyNodes
                .AsNoTracking()
                .Where(n => n.Id == node.ParentId)
                .Select(n => new { n.Code, n.Name })
                .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(new AssemblyNodeDetailDto
        {
            Id = node.Id.Value,
            Code = node.Code,
            Name = node.Name,
            ManualCode = node.ManualCode,
            Level = AssemblyNodeMapping.ToDto(node.Level),
            ParentId = node.ParentId?.Value,
            ParentCode = parent?.Code,
            ParentName = parent?.Name,
            Attributes = AssemblyNodeMapping.ToDto(node),
            IsActive = node.IsActive,
            BusinessUnitId = node.BusinessUnitId,
            CreatedAtUtc = node.CreatedAtUtc,
            ModifiedAtUtc = node.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(node.RowVersion),
        });
    }

    private static AssemblyNodeListItemDto ToDto(AssemblyNodeListRow row) => new()
    {
        Id = row.Id.Value,
        Code = row.Code,
        Name = row.Name,
        ManualCode = row.ManualCode,
        Level = AssemblyNodeMapping.ToDto(row.Level),
        ParentId = row.ParentId?.Value,
        ParentCode = row.ParentCode,
        ParentName = row.ParentName,
        ChildCount = row.ChildCount,
        MachineType = row.MachineType,
        DrivenBy = row.DrivenBy,
        DrawingPath = row.DrawingPath,
        TechnicalSpecification = row.TechnicalSpecification,
        Remark = row.Remark,
        Quantity = row.Quantity,
        WeightKg = row.WeightKg,
        DisplaySequence = row.DisplaySequence,
        IsActive = row.IsActive,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
