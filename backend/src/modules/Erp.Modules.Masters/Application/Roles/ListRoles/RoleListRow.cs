namespace Erp.Modules.Masters.Application.Roles.ListRoles;

/// <summary>The shape the database query projects into. See <c>SupplierListRow</c> for why it exists.</summary>
internal sealed record RoleListRow
{
    public required int Id { get; init; }

    public required string? RolesName { get; init; }

    public required int RoleId { get; init; }

    public required bool IsActive { get; init; }

    public required bool BypassBusinessUnit { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
