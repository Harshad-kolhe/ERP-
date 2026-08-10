using Erp.Api.Common.Results;

namespace Erp.Api.Common.Cqrs;

/// <summary>
/// Handles one query â€” one read that never changes state.
/// <para>
/// Queries run against the module's <c>DbContext</c> with tracking disabled and
/// project directly to a contract DTO. They do not load aggregates: reading a
/// whole object graph to render six columns is what made the legacy list screens
/// slow before they even reached the browser.
/// </para>
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
