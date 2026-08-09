namespace Erp.Contracts.Security;

/// <summary>One row of the roles grid.</summary>
public sealed record RoleListItemDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>How many permissions this role grants. The number people scan for.</summary>
    public required int PermissionCount { get; init; }

    /// <summary>How many users hold it — shown so nobody edits a role without knowing the blast radius.</summary>
    public required int UserCount { get; init; }

    /// <summary>Grants everything, including permissions added by future modules.</summary>
    public required bool IsSuperAdministrator { get; init; }
}

public sealed record RoleDetailDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>The permission codes granted. The authoritative mapping, held in the database.</summary>
    public required IReadOnlyList<string> Permissions { get; init; }

    public required int UserCount { get; init; }

    /// <summary>When true, Permissions is empty and irrelevant — the role grants everything.</summary>
    public required bool IsSuperAdministrator { get; init; }
}

public sealed record CreateRoleRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Codes must exist in the permission catalogue; unknown ones are rejected.</summary>
    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed record UpdateRoleRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The complete set after the edit, not a delta. The screen sends what the role
    /// should end up with, so a permission removed by one administrator cannot be
    /// silently reinstated by another who was working from a stale page.
    /// </summary>
    public required IReadOnlyList<string> Permissions { get; init; }
}
