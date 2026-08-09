using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Modules.Masters.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartOriginalPartNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalPartNumber",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill, added by hand. EF seeds a new non-nullable column from
            // default(string) — an empty string — but a part with no original number
            // is not a thing: a part that is not a revision is its own original.
            // Without this, every row already in the table would claim to belong to
            // a part family called "".
            migrationBuilder.Sql(
                """
                UPDATE [masters].[Part]
                SET [OriginalPartNumber] = [PartNumber]
                WHERE [OriginalPartNumber] = N'';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Part_BusinessUnit_OriginalPartNumber",
                schema: "masters",
                table: "Part",
                columns: new[] { "BusinessUnitId", "OriginalPartNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Part_BusinessUnit_OriginalPartNumber",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "OriginalPartNumber",
                schema: "masters",
                table: "Part");
        }
    }
}
