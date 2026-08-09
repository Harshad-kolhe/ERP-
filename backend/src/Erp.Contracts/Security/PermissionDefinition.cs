namespace Erp.Contracts.Security;

/// <summary>
/// One permission the system can grant, as the roles administration screen sees it.
/// <para>
/// Code declares which permissions <em>exist</em>, because an endpoint references
/// them and a typo has to be a compile error. Code never declares which role holds
/// them: that assignment is data, editable at runtime through the roles screen,
/// and it is the only place the mapping lives.
/// </para>
/// </summary>
/// <param name="Code">Stable identifier, e.g. <c>masters.part.approve</c>. Never renamed once shipped.</param>
/// <param name="Name">Human label for the roles screen, e.g. "Approve parts".</param>
/// <param name="Group">Grouping within the module, e.g. "Parts", so the screen is navigable.</param>
/// <param name="Module">Owning module, e.g. "Masters".</param>
public sealed record PermissionDefinition(string Code, string Name, string Group, string Module);
