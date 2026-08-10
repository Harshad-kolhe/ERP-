using Erp.Contracts.Security;

namespace Erp.Api.Common.Security;

/// <summary>
/// Every permission every module defines, in one place.
/// <para>
/// This is the catalogue the roles administration screen binds to. It answers
/// "what can be granted"; the database answers "what <em>is</em> granted, to whom".
/// Keeping those two questions apart is what makes the permission model
/// configurable at runtime without a deployment.
/// </para>
/// </summary>
public interface IPermissionCatalogue
{
    IReadOnlyList<PermissionDefinition> All { get; }

    /// <summary>True when the code is one the system actually defines.</summary>
    bool IsDefined(string code);
}

internal sealed class PermissionCatalogue : IPermissionCatalogue
{
    private readonly HashSet<string> _codes;

    public PermissionCatalogue(IEnumerable<IPermissionSource> sources)
    {
        All =
        [
            .. sources
                .SelectMany(source => source.Permissions)
                .OrderBy(permission => permission.Module, StringComparer.Ordinal)
                .ThenBy(permission => permission.Group, StringComparer.Ordinal)
                .ThenBy(permission => permission.Code, StringComparer.Ordinal),
        ];

        var duplicates = All
            .GroupBy(permission => permission.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            // Two modules claiming one code would make a grant ambiguous. Fail at
            // startup rather than let it surface as a permission that behaves
            // differently depending on which screen granted it.
            throw new InvalidOperationException(
                "These permission codes are defined by more than one module: "
                + string.Join(", ", duplicates));
        }

        _codes = All.Select(permission => permission.Code).ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyList<PermissionDefinition> All { get; }

    public bool IsDefined(string code) => _codes.Contains(code);
}
