namespace Erp.Api.Common.Entities;

/// <summary>
/// Marks an entity whose create/modify stamps are maintained automatically by
/// <c>AuditStampInterceptor</c>.
/// <para>
/// Implementing this interface is the whole of the work: the interceptor finds it,
/// so an entity cannot be added with the stamping "forgotten". In the legacy
/// system these columns were set by hand at each call site, and several screens
/// stamped every row as <c>CreatedBy = 1</c> because they read a claim that was
/// never issued.
/// </para>
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }

    Guid CreatedByUserId { get; set; }

    DateTimeOffset? ModifiedAtUtc { get; set; }

    Guid? ModifiedByUserId { get; set; }
}
