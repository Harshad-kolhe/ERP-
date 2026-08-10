using Erp.Api.Common.Entities;
using Erp.Api.Common.Results;

namespace Erp.Api.Domain.UnitsOfMeasure;

/// <summary>
/// A unit a quantity can be counted, weighed or measured in.
/// <para>
/// Promoted out of <c>LookupValue</c> because a unit is not a label. It carries two
/// things a four-column lookup row cannot hold, and both are needed the moment a
/// quantity is transacted rather than merely displayed:
/// </para>
/// <para>
/// <see cref="ConversionToBase"/> â€” a part bought in TON and stocked in KG is one
/// part, and something has to know the factor. Storing three unit codes on
/// <c>Part</c> (primary, purchase, selling) without it describes a conversion the
/// system cannot perform.
/// </para>
/// <para>
/// <see cref="Decimals"/> â€” 0.5 NOS is not a quantity. Precision belongs to the
/// unit, not to every column that happens to hold one.
/// </para>
/// <para>
/// Deliberately not <see cref="IBusinessUnitScoped"/>, for the same reason
/// <c>LookupValue</c> is not: a kilogram means the same thing in every business
/// unit.
/// </para>
/// </summary>
public sealed class UnitOfMeasure : AggregateRoot<int>, IAuditable, ISoftDeletable, IHasRowVersion
{
    /// <summary>What gets stored on the record that references it â€” <c>KG</c>, <c>NOS</c>.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>
    /// How many decimal places a quantity in this unit may have. Zero for anything
    /// counted: a part list asking for 2.5 bearings is a data-entry error, not a
    /// rounding problem to be discovered at the picking bay.
    /// </summary>
    public int Decimals { get; set; }

    /// <summary>
    /// The unit this one converts to, or null when it is itself the base of its
    /// family. TON's base is KG; KG's base is KG.
    /// <para>
    /// A code rather than a foreign key to keep it readable in a support query, and
    /// consistent with how every other reference is stored in this system.
    /// </para>
    /// </summary>
    public string? BaseUnitCode { get; set; }

    /// <summary>
    /// How many base units one of this unit is. 1000 for TON when the base is KG.
    /// Null means one, which is what a base unit converts at.
    /// <para>
    /// This holds only conversions that are true everywhere. A BOX of 12 for one
    /// part and 50 for another is a fact about the part, not about the box â€” see the
    /// note on the class about what is deliberately not here yet.
    /// </para>
    /// </summary>
    public decimal? ConversionToBase { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Retired units stay in the table and drop out of the dropdown. Deleting one
    /// would leave existing parts measured in a unit nothing can explain.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    /// <summary>The family this unit converts within. Its own code when it is the base.</summary>
    public string BaseCode => BaseUnitCode ?? Code;

    /// <summary>Base units per one of this unit. One for a base unit.</summary>
    public decimal FactorToBase => ConversionToBase ?? 1m;

    /// <summary>
    /// Restates a quantity in another unit.
    /// <para>
    /// Fails rather than guesses when the two units are not in the same family:
    /// there is no factor from KG to MTR, and returning the number unchanged â€” which
    /// is what a conversion helper that swallows the mismatch does â€” would put a
    /// weight into a length column and nothing would ever notice.
    /// </para>
    /// </summary>
    public static Result<decimal> ConvertQuantity(UnitOfMeasure from, UnitOfMeasure to, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (!string.Equals(from.BaseCode, to.BaseCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<decimal>(Error.Validation(
                "uom.not_convertible",
                $"There is no conversion from '{from.Code}' to '{to.Code}' â€” they measure different things."));
        }

        return Result.Success(quantity * from.FactorToBase / to.FactorToBase);
    }
}
