namespace Erp.Api.Common.Identity;

/// <summary>
/// Well-known principals for work that no human initiated.
/// <para>
/// Background jobs still write rows, and those rows still need an honest author.
/// Naming the actor explicitly is the alternative to what the legacy system did:
/// three modules read a claim that was never issued, so every record they created
/// was stamped <c>CreatedBy = 1</c> â€” a real person who had not touched it.
/// </para>
/// <para>
/// The <c>ffffffff</c> prefix keeps these outside any range the identity provider
/// will allocate, so they can never collide with a real user.
/// </para>
/// </summary>
public static class SystemUsers
{
    /// <summary>Recurring and queued background jobs (nightly stock snapshot, email scheduler).</summary>
    public static readonly Guid Background = new("ffffffff-0000-0000-0000-000000000001");

    /// <summary>Schema migrations and reference-data seeding.</summary>
    public static readonly Guid Seed = new("ffffffff-0000-0000-0000-000000000002");

    /// <summary>Records created by the legacy-data migration ETL.</summary>
    public static readonly Guid DataMigration = new("ffffffff-0000-0000-0000-000000000003");
}
