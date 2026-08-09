using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Modules.Masters.Infrastructure.Migrations
{
    /// <summary>
    /// Retires the <c>Inactive</c> status in favour of the <c>IsActive</c> flag.
    /// <para>
    /// EF generated this empty: the column is still <c>nvarchar(20)</c> and nothing
    /// about its <em>shape</em> changed. What changed is which values are legal in
    /// it, and a row still holding <c>'Inactive'</c> now has no enum member to
    /// materialise into — the read throws rather than returning a wrong answer, so
    /// leaving this empty would take those records out of the application entirely.
    /// The schema was never the migration here; the data is.
    /// </para>
    /// </summary>
    public partial class RetireInactiveStatus : Migration
    {
        /// <summary>Every master that carries the shared approval status.</summary>
        private static readonly string[] Tables = ["Part", "Supplier", "Customer", "Employee"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * 'Inactive' said two things at once: not approved for use, and not in
             * use. The second is now IsActive, and it is set here. The first cannot
             * be recovered — the old status overwrote whatever the record was before
             * it was withdrawn, which is precisely the information loss that made
             * splitting these two fields worth doing.
             *
             * So the approval state resets to Draft rather than guessing Approved.
             * Draft claims nothing: the record is out of use and unapproved, and
             * putting it back into service means approving it again. Guessing
             * Approved would be inventing a sign-off that may never have happened,
             * on a record that would then be usable the moment somebody reactivated
             * it.
             */
            foreach (var table in Tables)
            {
                migrationBuilder.Sql(
                    $"""
                    UPDATE [masters].[{table}]
                    SET [Status] = 'Draft',
                        [IsActive] = 0
                    WHERE [Status] = 'Inactive';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately not reversed. Going back would have to pick rows to call
            // 'Inactive' again, and after Up ran there is nothing left that
            // identifies which ones they were — every withdrawn record now looks the
            // same as one that was drafted and never submitted. A Down that
            // re-labelled all of them would corrupt data that Up did not touch.
        }
    }
}
