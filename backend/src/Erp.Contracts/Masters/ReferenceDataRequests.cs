namespace Erp.Contracts.Masters;

/// <summary>
/// The editable fields of one option in a list.
/// <para>
/// <c>Type</c> is not here: a lookup value cannot be moved between lists after it
/// is created. Doing so would silently reinterpret every record that already
/// stores the code — a part whose source code is <c>OutSource</c> does not become
/// a part with a payment term because somebody re-filed the option.
/// </para>
/// </summary>
public record SaveLookupValueRequest
{
    /// <summary>What the user sees in the dropdown.</summary>
    public required string Name { get; init; }

    /// <summary>Where in the list it appears. Lists have a natural order that is not alphabetical.</summary>
    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateLookupValueRequest : SaveLookupValueRequest
{
    /// <summary>Which list this belongs to — <c>moc</c>, <c>part.type</c>, <c>paymentTerms</c>.</summary>
    public required string Type { get; init; }

    /// <summary>What gets stored on the records that reference it. Fixed once created.</summary>
    public required string Code { get; init; }
}

public sealed record UpdateLookupValueRequest : SaveLookupValueRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>One row of the reference-data grid.</summary>
public sealed record LookupValueListItemDto
{
    public required int Id { get; init; }

    public required string? Type { get; init; }

    public required string? Code { get; init; }

    public required string? Name { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>One lookup value, as the edit screen loads it.</summary>
public sealed record LookupValueDetailDto
{
    public required int Id { get; init; }

    public required string? Type { get; init; }

    public required string? Code { get; init; }

    public required string? Name { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}

/// <summary>
/// The editable fields of a unit of measure.
/// <para>
/// The code is absent for the same reason it is absent from a lookup value: every
/// part measured in <c>KG</c> stores the letters, not a key, so renaming the unit
/// would orphan them.
/// </para>
/// </summary>
public record SaveUnitOfMeasureRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// Decimal places a quantity in this unit may have. Zero for anything counted —
    /// half a bearing is a typing error, not a rounding problem.
    /// </summary>
    public int Decimals { get; init; }

    /// <summary>
    /// The unit this one converts to. Blank means it is itself the base of its
    /// family, which is what most units are.
    /// </summary>
    public string? BaseUnitCode { get; init; }

    /// <summary>How many base units one of this unit is — 1000 for TON when the base is KG.</summary>
    public decimal? ConversionToBase { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateUnitOfMeasureRequest : SaveUnitOfMeasureRequest
{
    /// <summary>What parts and documents store. Fixed once created.</summary>
    public required string Code { get; init; }
}

public sealed record UpdateUnitOfMeasureRequest : SaveUnitOfMeasureRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

public sealed record UnitOfMeasureListItemDto
{
    public required int Id { get; init; }

    public required string? Code { get; init; }

    public required string? Name { get; init; }

    public required int Decimals { get; init; }

    public required string? BaseUnitCode { get; init; }

    public required decimal? ConversionToBase { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record UnitOfMeasureDetailDto
{
    public required int Id { get; init; }

    public required string? Code { get; init; }

    public required string? Name { get; init; }

    public required int Decimals { get; init; }

    public required string? BaseUnitCode { get; init; }

    public required decimal? ConversionToBase { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}

/// <summary>The editable fields of an HSN code. Its rates are added separately — see <see cref="AddHsnGstRateRequest"/>.</summary>
public record SaveHsnCodeRequest
{
    public required string Description { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateHsnCodeRequest : SaveHsnCodeRequest
{
    /// <summary>4, 6 or 8 digits. Fixed once created.</summary>
    public required string Code { get; init; }

    /// <summary>
    /// The rate to open the code with, and the date it applies from.
    /// <para>
    /// Required, because a code with no rate cannot tax anything: it would pass the
    /// existence check on a part and then produce an invoice line with no GST on it.
    /// A code that genuinely attracts none is 0, stated.
    /// </para>
    /// </summary>
    public required decimal RatePercent { get; init; }

    public required DateOnly EffectiveFrom { get; init; }
}

public sealed record UpdateHsnCodeRequest : SaveHsnCodeRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>
/// A rate change. Added, never edited.
/// <para>
/// There is deliberately no way to change or delete an existing rate. The whole
/// reason the rates are a table is that an invoice raised last March must still
/// price at last March's rate; a screen that let someone correct the old row would
/// rewrite the tax on every document that reads it.
/// </para>
/// </summary>
public sealed record AddHsnGstRateRequest
{
    public required decimal RatePercent { get; init; }

    public required DateOnly EffectiveFrom { get; init; }
}

/// <summary>One rate, and the date it took effect.</summary>
public sealed record HsnGstRateDto
{
    public required decimal RatePercent { get; init; }

    public required DateOnly EffectiveFrom { get; init; }
}

public sealed record HsnCodeListItemDto
{
    public required int Id { get; init; }

    public required string? Code { get; init; }

    public required string? Description { get; init; }

    /// <summary>The rate in force today. Null while the code has none yet.</summary>
    public required decimal? CurrentRatePercent { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record HsnCodeDetailDto
{
    public required int Id { get; init; }

    public required string? Code { get; init; }

    public required string? Description { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Every rate this code has attracted, newest first.</summary>
    public required IReadOnlyList<HsnGstRateDto> Rates { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}
