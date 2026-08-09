namespace Erp.Contracts.Masters;

/// <summary>One selectable option. The client renders these and never invents any.</summary>
public sealed record LookupOptionDto
{
    /// <summary>Stored on the record that references it.</summary>
    public required string Code { get; init; }

    /// <summary>Shown to the user. Often identical to <see cref="Code"/>.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// The reply to a lookup request: the lists that were asked for, keyed by name.
/// <para>
/// One request carries every list a form needs. A supplier form wants six of them,
/// and six round trips to fill one screen is how a form ends up rendering before
/// its dropdowns do.
/// </para>
/// </summary>
public sealed record LookupSetDto
{
    public required IReadOnlyDictionary<string, IReadOnlyList<LookupOptionDto>> Lookups { get; init; }
}
