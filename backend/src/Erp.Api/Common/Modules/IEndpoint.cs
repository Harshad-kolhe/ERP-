using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Common.Modules;

/// <summary>
/// One HTTP endpoint, in one file.
/// <para>
/// This is the structural answer to the 4,686-line <c>MastersController</c> with
/// 249 actions and 41 constructor dependencies. A controller is an unbounded
/// bucket that grows until it is unreviewable and every developer's change
/// conflicts with every other. A file that holds a single endpoint has nowhere
/// to grow, and two people adding features touch two different files.
/// </para>
/// <para>
/// Implementations need a parameterless constructor: the endpoint's dependencies
/// are injected into the handler delegate by Minimal API parameter binding, not
/// into the mapping class.
/// </para>
/// </summary>
public interface IEndpoint
{
    /// <summary>Registers this endpoint on its module's route group.</summary>
    void Map(RouteGroupBuilder group);
}
