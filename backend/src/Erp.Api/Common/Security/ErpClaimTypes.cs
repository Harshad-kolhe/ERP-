namespace Erp.Api.Common.Security;

/// <summary>Claim types this application issues and reads.</summary>
public static class ErpClaimTypes
{
    public const string UserId = "erp:uid";

    public const string UserName = "erp:uname";

    /// <summary>One claim per granted permission code. Flattened from the user's roles at sign-in.</summary>
    public const string Permission = "erp:perm";

    /// <summary>The business unit this session operates in.</summary>
    public const string BusinessUnit = "erp:bu";

    /// <summary>Present when the principal may read across every business unit.</summary>
    public const string AllBusinessUnits = "erp:bu_all";

    /// <summary>
    /// Present when the principal holds a super-administrator role.
    /// <para>
    /// Informational: the permission claims have already been expanded from the
    /// catalogue by the time this is issued, so no authorization check needs to
    /// consult it. It exists so the interface can say "all access" instead of
    /// listing forty permissions, and so an audit log can record why.
    /// </para>
    /// </summary>
    public const string SuperAdministrator = "erp:superadmin";
}
