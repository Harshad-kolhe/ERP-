using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Assemblies.WriteAssemblyNode;

/// <param name="Level">
/// From the route, never from the body. It is part of the identity of the request:
/// asking <c>/masters/sections/{id}</c> for a sub-assembly must 404, or the three
/// screens' permissions become one permission.
/// </param>
internal sealed record GetAssemblyNodeByIdQuery(AssemblyLevel Level, Guid Id);

internal sealed record CreateAssemblyNodeCommand(AssemblyLevel Level, CreateAssemblyNodeRequest Request);

internal sealed record UpdateAssemblyNodeCommand(AssemblyLevel Level, Guid Id, UpdateAssemblyNodeRequest Request);

internal sealed class GetAssemblyNodeByIdHandler(MastersDbContext db)
    : IQueryHandler<GetAssemblyNodeByIdQuery, AssemblyNodeDetailDto>
{
    public async Task<Result<AssemblyNodeDetailDto>> HandleAsync(
        GetAssemblyNodeByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = new AssemblyNodeId(query.Id);

        var node = await db.AssemblyNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.Level == query.Level, cancellationToken);

        if (node is null)
        {
            return Result.Failure<AssemblyNodeDetailDto>(AssemblyErrors.NotFound(query.Level, query.Id));
        }

        // A second, tiny read rather than a join: the form needs the parent's code
        // and name to label its picker, and the alternative — a navigation property
        // — would be one an unrelated list query could accidentally include.
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
}

internal sealed class CreateAssemblyNodeHandler(MastersDbContext db)
    : ICommandHandler<CreateAssemblyNodeCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateAssemblyNodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var level = command.Level;
        var request = command.Request;
        var code = Normalize.RequiredCode(request.Code);

        var parent = await AssemblyNodeRules.ResolveParentAsync(db, level, request.ParentId, cancellationToken);

        if (parent.IsFailure)
        {
            return Result.Failure<Guid>(parent.Error);
        }

        // Checked here so the user gets a precise message rather than a database
        // error. The unique indexes are still what guarantee both — see the catch.
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
        catch (DbUpdateException exception) when (AssemblyNodeRules.IsUniqueViolation(exception))
        {
            // Two requests passed the checks above concurrently and an index rejected
            // the loser. The constraint is the source of truth, not the read — which
            // is exactly what the legacy "read the max and add one" code lacked.
            return Result.Failure<Guid>(AssemblyErrors.DuplicateCode(code));
        }

        return Result.Success(created.Value.Id.Value);
    }
}

internal sealed class UpdateAssemblyNodeHandler(MastersDbContext db)
    : ICommandHandler<UpdateAssemblyNodeCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateAssemblyNodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var level = command.Level;
        var request = command.Request;

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(AssemblyErrors.StaleRowVersion);
        }

        var id = new AssemblyNodeId(command.Id);

        var node = await db.AssemblyNodes
            .FirstOrDefaultAsync(n => n.Id == id && n.Level == level, cancellationToken);

        if (node is null)
        {
            return Result.Failure<Unit>(AssemblyErrors.NotFound(level, command.Id));
        }

        var parent = await AssemblyNodeRules.ResolveParentAsync(db, level, request.ParentId, cancellationToken);

        if (parent.IsFailure)
        {
            return Result.Failure<Unit>(parent.Error);
        }

        var name = request.Name.Trim();
        var parentId = parent.Value;

        if (await db.AssemblyNodes
                .AsNoTracking()
                .AnyAsync(
                    n => n.Id != id && n.Level == level && n.ParentId == parentId && n.Name == name,
                    cancellationToken))
        {
            return Result.Failure<Unit>(AssemblyErrors.DuplicateName(level, name));
        }

        // Withdrawing a node that things still hang off would leave those children
        // selectable under a parent that is not. The legacy screen allowed it, and
        // the orphans showed up on drawings for years.
        if (!request.IsActive && node.IsActive)
        {
            var activeChildren = await db.AssemblyNodes
                .AsNoTracking()
                .CountAsync(child => child.ParentId == id && child.IsActive, cancellationToken);

            if (activeChildren > 0)
            {
                return Result.Failure<Unit>(AssemblyErrors.HasActiveChildren(activeChildren));
            }
        }

        // Tell EF the version the client was looking at. If the row has moved on,
        // the UPDATE matches zero rows and EF raises rather than silently discarding
        // the other person's edit.
        db.Entry(node).Property(n => n.RowVersion).OriginalValue = rowVersion;

        var updated = node.Update(
            parentId,
            name,
            request.IsActive,
            AssemblyNodeMapping.ToDomain(request.Attributes));

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
            return Result.Failure<Unit>(AssemblyErrors.StaleRowVersion);
        }
        catch (DbUpdateException exception) when (AssemblyNodeRules.IsUniqueViolation(exception))
        {
            return Result.Failure<Unit>(AssemblyErrors.DuplicateName(level, name));
        }

        return Result.Success(Unit.Value);
    }
}

/// <summary>
/// The checks create and update share. One copy, because the legacy system had
/// three and they had already drifted: sections validated nothing, assemblies
/// checked that the parent existed and was a section, and sub-assemblies accepted
/// either a section or an assembly plus a rule about siblings that no other level
/// applied.
/// </summary>
internal static class AssemblyNodeRules
{
    /// <summary>
    /// Turns the requested parent id into a validated <see cref="AssemblyNodeId"/>,
    /// or explains why it cannot be one.
    /// <para>
    /// Success carries <c>null</c> for a section, which legitimately has no parent —
    /// the presence rules themselves are checked here so the caller never has to ask
    /// twice.
    /// </para>
    /// </summary>
    public static async Task<Result<AssemblyNodeId?>> ResolveParentAsync(
        MastersDbContext db,
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

        // Reads through the tenancy filter, so a parent in another business unit is
        // indistinguishable from one that does not exist.
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

    /// <summary>SQL Server 2601 (unique index) and 2627 (unique constraint).</summary>
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
