namespace Erp.Modules.Masters.Infrastructure.ReadModels;

/// <summary>
/// Just enough of a user to put a name in a "Created by" column.
/// <para>
/// Every master grid in the legacy system shows who created and last changed a row.
/// The audit interceptor stores a user id, so rendering that needs a name, and the
/// two obvious ways to get one are both worse than this. Returning the raw
/// <see cref="Guid"/> and resolving it in the browser turns one grid page into
/// n+1 requests and puts a join in the client — the exact pattern this rewrite
/// exists to remove. Copying the display name onto every master row at write time
/// denormalises it into six tables that then disagree the first time somebody is
/// renamed.
/// </para>
/// <para>
/// So the list handlers left-join this instead, and the name arrives in the same
/// single SELECT as the rest of the page. It is mapped as a <em>view</em> over
/// <c>identity.AspNetUsers</c>, which is deliberate on three counts: EF never
/// generates migrations for a view, so this module cannot alter a table it does not
/// own; the mapping is keyless and read-only, so nothing here can write one; and
/// both contexts share one database, so the join costs nothing extra.
/// </para>
/// <para>
/// It is a read model, not a dependency on the identity module's behaviour. If
/// identity moves to its own database the fix is local: this becomes a synchronised
/// projection in the <c>masters</c> schema and no handler changes.
/// </para>
/// </summary>
internal sealed class AuditUser
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;
}
