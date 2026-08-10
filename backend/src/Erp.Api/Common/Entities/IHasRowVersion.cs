namespace Erp.Api.Common.Entities;

/// <summary>
/// Marks an aggregate root that participates in optimistic concurrency.
/// The column is configured as a SQL Server <c>rowversion</c> by convention;
/// a concurrent overwrite surfaces as HTTP 409 rather than silently winning.
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
