using Erp.Api.Common.Results;
using Erp.Api.Domain.Assemblies;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Assemblies;

public sealed class AssemblyNodesService(ErpDbContext db)
{
    public async Task<Result<Guid>> CreateAsync(
        AssemblyLevel level,
        CreateAssemblyNodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = Normalize.RequiredCode(request.Code);

        var parent = await ResolveParentAsync(level, request.ParentId, cancellationToken);

        if (parent.IsFailure)
        {
            return Result.Failure<Guid>(parent.Error);
        }

        if (await db.AssemblyNodes.AsNoTracking().AnyAsync(n => n.Code == code, cancellationToken))
        {
            return Result.Failure<Guid>(AssemblyErrors.DuplicateCode(code));
        }

        var name = request.Name.Trim();
        var parentId = parent.Value;

        if (await db.AssemblyNodes
                .AsNoTracking()
                .AnyAsync(n => n.Level == level && n.ParentId == parentId && n.Name == name, cancellationToken))
        {
            return Result.Failure<Guid>(AssemblyErrors.DuplicateName(level, name));
        }

        var created = AssemblyNode.Create(
            level,
            parentId,
            code,
            name,
            AssemblyNodeMapping.ToDomain(request.Attributes));

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        db.AssemblyNodes.Add(created.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<Guid>(AssemblyErrors.DuplicateCode(code));
        }

        return Result.Success(created.Value.Id.Value);
    }

    public async Task<Result> UpdateAsync(
        AssemblyLevel level,
        Guid id,
        UpdateAssemblyNodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(AssemblyErrors.StaleRowVersion);
        }

        var nodeId = new AssemblyNodeId(id);

        var node = await db.AssemblyNodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.Level == level, cancellationToken);

        if (node is null)
        {
            return Result.Failure(AssemblyErrors.NotFound(level, id));
        }

        var parent = await ResolveParentAsync(level, request.ParentId, cancellationToken);

        if (parent.IsFailure)
        {
            return Result.Failure(parent.Error);
        }

        var name = request.Name.Trim();
        var parentId = parent.Value;

        if (await db.AssemblyNodes
                .AsNoTracking()
                .AnyAsync(
                    n => n.Id != nodeId && n.Level == level && n.ParentId == parentId && n.Name == name,
                    cancellationToken))
        {
            return Result.Failure(AssemblyErrors.DuplicateName(level, name));
        }

        if (!request.IsActive && node.IsActive)
        {
            var activeChildren = await db.AssemblyNodes
                .AsNoTracking()
                .CountAsync(child => child.ParentId == nodeId && child.IsActive, cancellationToken);

            if (activeChildren > 0)
            {
                return Result.Failure(AssemblyErrors.HasActiveChildren(activeChildren));
            }
        }

        db.Entry(node).Property(n => n.RowVersion).OriginalValue = rowVersion;

        var updated = node.Update(
            parentId,
            name,
            request.IsActive,
            AssemblyNodeMapping.ToDomain(request.Attributes));

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
            return Result.Failure(AssemblyErrors.StaleRowVersion);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure(AssemblyErrors.DuplicateName(level, name));
        }

        return Result.Success();
    }

    private async Task<Result<AssemblyNodeId?>> ResolveParentAsync(
        AssemblyLevel level,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var requiredParentLevel = AssemblyLevels.ParentOf(level);

        if (requiredParentLevel is null)
        {
            return parentId is null
                ? Result.Success<AssemblyNodeId?>(null)
                : Result.Failure<AssemblyNodeId?>(AssemblyErrors.ParentNotAllowed(level));
        }

        if (parentId is null)
        {
            return Result.Failure<AssemblyNodeId?>(AssemblyErrors.ParentRequired(level));
        }

        var id = new AssemblyNodeId(parentId.Value);

        var parentLevel = await db.AssemblyNodes
            .AsNoTracking()
            .Where(node => node.Id == id)
            .Select(node => (AssemblyLevel?)node.Level)
            .FirstOrDefaultAsync(cancellationToken);

        if (parentLevel is null)
        {
            return Result.Failure<AssemblyNodeId?>(AssemblyErrors.ParentNotFound(parentId.Value));
        }

        if (parentLevel != requiredParentLevel)
        {
            return Result.Failure<AssemblyNodeId?>(AssemblyErrors.ParentWrongLevel(level, parentLevel.Value));
        }

        return Result.Success<AssemblyNodeId?>(id);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
