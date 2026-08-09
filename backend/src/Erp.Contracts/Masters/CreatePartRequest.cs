namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for creating a part. Validated server-side by <c>CreatePartValidator</c>,
/// which is the single authority — the matching Zod schema in the web app mirrors
/// it for user experience only and is never trusted.
/// </summary>
public sealed record CreatePartRequest
{
    public required string PartNumber { get; init; }

    public required string Description { get; init; }

    public Guid? CategoryId { get; init; }

    public required string UnitOfMeasureCode { get; init; }

    public string? HsnCode { get; init; }

    public string? DrawingNumber { get; init; }

    /// <summary>
    /// The descriptive fields. Omitted entirely, the part is created from its
    /// identity alone — which is the normal path when a number is raised before the
    /// design is finalised.
    /// </summary>
    public PartAttributesDto? Attributes { get; init; }
}
