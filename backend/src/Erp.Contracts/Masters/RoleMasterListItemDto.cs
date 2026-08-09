namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the roles grid.
/// <para>
/// This is the legacy role master, which does <em>not</em> grant permissions —
/// authorisation runs on Identity roles. The distinction matters on screen, so the
/// UI labels it rather than letting an administrator assume otherwise.
/// </para>
/// </summary>
public sealed record RoleMasterListItemDto
{
    public required int Id { get; init; }

    public required string? RolesName { get; init; }

    public required int RoleId { get; init; }

    public required bool IsActive { get; init; }

    public required bool BypassBusinessUnit { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
