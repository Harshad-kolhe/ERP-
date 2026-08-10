namespace Erp.Api.Common.Entities;

/// <summary>
/// Marks an entity that is never physically deleted. A global query filter is
/// applied automatically by convention, so deleted rows disappear from every
/// query without any call site opting in.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAtUtc { get; set; }

    Guid? DeletedByUserId { get; set; }
}
