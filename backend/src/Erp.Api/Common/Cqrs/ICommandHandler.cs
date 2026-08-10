using Erp.Api.Common.Results;

namespace Erp.Api.Common.Cqrs;

/// <summary>
/// Handles one command â€” one state-changing operation.
/// <para>
/// There is deliberately no mediator and no runtime dispatcher: an endpoint
/// injects the exact handler it needs, so the dependency is visible in the
/// signature, resolvable by "go to definition", and verified by the compiler.
/// The legacy equivalent was a 5,000-line <c>BLL</c> class with 98 public members
/// that every controller could reach into.
/// </para>
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
