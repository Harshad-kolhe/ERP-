using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Modules.Masters.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyPartMasterAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormCategory",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoldRemark",
                schema: "masters",
                table: "Part",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InactiveRemark",
                schema: "masters",
                table: "Part",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Hand-corrected from the generated `defaultValue: false`. EF derives the
            // backfill value from `default(bool)`, not from the property initialiser,
            // so the generated version would have deactivated every part already in
            // the table. New parts are active; so were the existing ones.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "masters",
                table: "Part",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNumber",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                schema: "masters",
                table: "Part",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumStockLevel",
                schema: "masters",
                table: "Part",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Moc",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartCategoryCode",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartRevisionNo",
                schema: "masters",
                table: "Part",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartType",
                schema: "masters",
                table: "Part",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseUomCode",
                schema: "masters",
                table: "Part",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReorderPoint",
                schema: "masters",
                table: "Part",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisionRemark",
                schema: "masters",
                table: "Part",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellingUomCode",
                schema: "masters",
                table: "Part",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeriesCode",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                schema: "masters",
                table: "Part",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSpecification",
                schema: "masters",
                table: "Part",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                schema: "masters",
                table: "Part",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormCategory",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "HoldRemark",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "InactiveRemark",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "ItemNumber",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "MinimumStockLevel",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "Moc",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "PartCategoryCode",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "PartRevisionNo",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "PartType",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "PurchaseUomCode",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "RevisionRemark",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "SellingUomCode",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "SeriesCode",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "SourceCode",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "TechnicalSpecification",
                schema: "masters",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                schema: "masters",
                table: "Part");
        }
    }
}
