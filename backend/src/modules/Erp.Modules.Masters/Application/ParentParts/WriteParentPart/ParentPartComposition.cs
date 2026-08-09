using Erp.Contracts.Masters;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.ParentParts.WriteParentPart;

/// <summary>
/// The checks create and update share: that every part named actually exists, that
/// none of them is the parent itself, and that none appears twice.
/// <para>
/// None of these existed in the legacy screen. Both sides of the relationship were
/// part numbers stored as free text, so a typo silently produced a build referring
/// to nothing; the same child could be added repeatedly and each copy counted
/// towards the totals; and a part could be listed as its own component, which any
/// BOM explosion walks forever.
/// </para>
/// </summary>
internal static class ParentPartComposition
{
    /// <summary>
    /// A build is tens of lines. This bound exists so a hand-crafted request cannot
    /// ask the server to validate and insert an unbounded list in one transaction.
    /// </summary>
    public const int MaxComponents = 500;

    /// <summary>
    /// Validates the requested component lines against the part master and turns
    /// them into domain drafts.
    /// </summary>
    /// <param name="parentPartId">
    /// The part being built, so a line naming it can be rejected by name rather than
    /// by id.
    /// </param>
    public static async Task<Result<List<ParentPartComponentDraft>>> BuildAsync(
        MastersDbContext db,
        PartId parentPartId,
        IReadOnlyList<ParentPartComponentDto> components,
        CancellationToken cancellationToken)
    {
        var ids = components.Select(component => new PartId(component.PartId)).ToList();

        // One query for every part named, rather than one per line. The numbers come
        // back with them so every failure below can say which part it means — an
        // error quoting a Guid is one the user cannot act on.
        var numbersById = await db.Parts
            .AsNoTracking()
            .Where(part => ids.Contains(part.Id))
            .Select(part => new { part.Id, part.PartNumber })
            .ToDictionaryAsync(part => part.Id, part => part.PartNumber, cancellationToken);

        var seen = new HashSet<PartId>();
        var drafts = new List<ParentPartComponentDraft>(components.Count);

        foreach (var component in components)
        {
            var partId = new PartId(component.PartId);

            // Reads through the tenancy filter, so a part in another business unit
            // is indistinguishable from one that does not exist.
            if (!numbersById.TryGetValue(partId, out var partNumber))
            {
                return Result.Failure<List<ParentPartComponentDraft>>(
                    ParentPartErrors.ComponentNotFound(component.PartId));
            }

            if (partId == parentPartId)
            {
                return Result.Failure<List<ParentPartComponentDraft>>(
                    ParentPartErrors.ComponentIsParent(partNumber));
            }

            if (!seen.Add(partId))
            {
                return Result.Failure<List<ParentPartComponentDraft>>(
                    ParentPartErrors.DuplicateComponent(partNumber));
            }

            if (component.Quantity <= 0)
            {
                return Result.Failure<List<ParentPartComponentDraft>>(
                    ParentPartErrors.QuantityMustBePositive(partNumber));
            }

            drafts.Add(new ParentPartComponentDraft(
                partId,
                component.Quantity,
                component.UnitOfMeasureCode,
                component.UnitWeightKg,
                component.Rate,
                component.DrawingNumber,
                component.Remark));
        }

        return Result.Success(drafts);
    }

    /// <summary>
    /// Resolves the optional assembly node a build is filed under.
    /// <para>
    /// Any level is accepted: a bought-in gearbox hangs off a sub-assembly, a
    /// weldment off a section. What is not accepted is a code that does not resolve
    /// — which the legacy free-text column could not tell apart from one that did.
    /// </para>
    /// </summary>
    public static async Task<Result<AssemblyNodeId?>> ResolveAssemblyNodeAsync(
        MastersDbContext db,
        Guid? assemblyNodeId,
        CancellationToken cancellationToken)
    {
        if (assemblyNodeId is null)
        {
            return Result.Success<AssemblyNodeId?>(null);
        }

        var id = new AssemblyNodeId(assemblyNodeId.Value);

        var exists = await db.AssemblyNodes
            .AsNoTracking()
            .AnyAsync(node => node.Id == id, cancellationToken);

        return exists
            ? Result.Success<AssemblyNodeId?>(id)
            : Result.Failure<AssemblyNodeId?>(AssemblyErrors.ParentNotFound(assemblyNodeId.Value));
    }
}
