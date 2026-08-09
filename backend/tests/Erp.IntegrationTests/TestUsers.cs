using Erp.Modules.Masters.Integration;

namespace Erp.IntegrationTests;

/// <summary>
/// Fixtures seeded once per test run.
/// <para>
/// Deliberately several users rather than one omnipotent account: the point of
/// these tests is to prove that permissions and tenancy are enforced on the
/// server, and a single all-powerful user cannot demonstrate that.
/// </para>
/// </summary>
public static class TestUsers
{
    /// <summary>Satisfies the Identity policy: 12+ characters, mixed case, digit, symbol.</summary>
    public const string Password = "Test!Passw0rd123";

    public const int BusinessUnitOne = 1;
    public const int BusinessUnitTwo = 2;

    /// <summary>Full Masters permissions, business unit 1. Authors parts.</summary>
    public static readonly TestUser Author = new(
        "author@erp.test",
        BusinessUnitOne,
        [MastersPermissions.PartRead, MastersPermissions.PartCreate, MastersPermissions.PartUpdate, MastersPermissions.PartSubmit]);

    /// <summary>Business unit 1, approval rights only. Exists to prove segregation of duties.</summary>
    public static readonly TestUser Approver = new(
        "approver@erp.test",
        BusinessUnitOne,
        [MastersPermissions.PartRead, MastersPermissions.PartApprove]);

    /// <summary>Business unit 1, read only. Exists to prove 403 is real.</summary>
    public static readonly TestUser Reader = new(
        "reader@erp.test",
        BusinessUnitOne,
        [MastersPermissions.PartRead]);

    /// <summary>Business unit 2, full permissions. Exists to prove tenant isolation.</summary>
    public static readonly TestUser OtherUnit = new(
        "other@erp.test",
        BusinessUnitTwo,
        [MastersPermissions.PartRead, MastersPermissions.PartCreate, MastersPermissions.PartUpdate]);

    /// <summary>
    /// Business unit 1, maintains the machine breakdown and the parent-part builds.
    /// <para>
    /// Carries part read and create as well, because a build has to name parts that
    /// exist — which is itself the point: the parent-part endpoints refuse a part id
    /// they cannot resolve, so a test that could not create parts could not
    /// distinguish "rejected because the rule works" from "rejected because nothing
    /// was there".
    /// </para>
    /// </summary>
    public static readonly TestUser Engineer = new(
        "engineer@erp.test",
        BusinessUnitOne,
        [
            MastersPermissions.PartRead,
            MastersPermissions.PartCreate,
            MastersPermissions.SectionRead,
            MastersPermissions.SectionCreate,
            MastersPermissions.SectionUpdate,
            MastersPermissions.AssemblyRead,
            MastersPermissions.AssemblyCreate,
            MastersPermissions.AssemblyUpdate,
            MastersPermissions.SubAssemblyRead,
            MastersPermissions.SubAssemblyCreate,
            MastersPermissions.SubAssemblyUpdate,
            MastersPermissions.ParentPartRead,
            MastersPermissions.ParentPartCreate,
            MastersPermissions.ParentPartUpdate,
        ]);

    /// <summary>
    /// Business unit 1, may read and create sections but nothing below them. Exists
    /// to prove the three levels really are three permissions — the whole reason
    /// they are not one <c>masters.assembly.*</c> grant over a shared table.
    /// </summary>
    public static readonly TestUser SectionOnly = new(
        "sections@erp.test",
        BusinessUnitOne,
        [MastersPermissions.SectionRead, MastersPermissions.SectionCreate]);

    /// <summary>
    /// Business unit 1, maintains the code lists.
    /// <para>
    /// Carries part create as well, because the point of the reference-data screens
    /// is what happens downstream: an option added here has to become one a part
    /// will accept, and a user who could not save a part could not show that.
    /// </para>
    /// </summary>
    public static readonly TestUser Librarian = new(
        "librarian@erp.test",
        BusinessUnitOne,
        [
            MastersPermissions.ReferenceDataRead,
            MastersPermissions.ReferenceDataCreate,
            MastersPermissions.ReferenceDataUpdate,
            MastersPermissions.PartRead,
            MastersPermissions.PartCreate,
        ]);

    public static IReadOnlyList<TestUser> All { get; } =
        [Author, Approver, Reader, OtherUnit, Engineer, SectionOnly, Librarian];
}

public sealed record TestUser(string UserName, int BusinessUnitId, IReadOnlyList<string> Permissions)
{
    /// <summary>One role per user keeps the permission sets independent.</summary>
    public string RoleName => $"role-{UserName.Split('@')[0]}";
}
