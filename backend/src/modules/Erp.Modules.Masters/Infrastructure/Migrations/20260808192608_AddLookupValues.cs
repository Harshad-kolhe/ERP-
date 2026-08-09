using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Modules.Masters.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LookupValue",
                schema: "masters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookupValue_IsDeleted",
                schema: "masters",
                table: "LookupValue",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValue_Type_IsActive_SortOrder",
                schema: "masters",
                table: "LookupValue",
                columns: new[] { "Type", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_LookupValue_Type_Code",
                schema: "masters",
                table: "LookupValue",
                columns: new[] { "Type", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            SeedStartingOptions(migrationBuilder);
        }

        /// <summary>
        /// A starting set of options, so the forms are usable the moment the table
        /// exists.
        /// <para>
        /// This is reference data, not configuration: it ships once and is edited in
        /// the application afterwards. That is the whole difference from the legacy
        /// system, where the same lists were written into JavaScript and adding a
        /// payment term meant a deployment.
        /// </para>
        /// <para>
        /// The timestamp is a literal rather than <c>SYSDATETIMEOFFSET()</c> so that
        /// running this migration twice on two machines produces identical rows —
        /// the same property the schema diagrams rely on.
        /// </para>
        /// </summary>
        private static void SeedStartingOptions(MigrationBuilder migrationBuilder)
        {
            var order = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (type, code, name) in StartingOptions)
            {
                order[type] = order.TryGetValue(type, out var previous) ? previous + 1 : 1;

                migrationBuilder.InsertData(
                    schema: "masters",
                    table: "LookupValue",
                    columns: new[]
                    {
                        "Type", "Code", "Name", "SortOrder", "IsActive",
                        "CreatedAtUtc", "CreatedByUserId", "IsDeleted",
                    },
                    values: new object[]
                    {
                        type, code, name, order[type], true, SeededAt, SeedUserId, false,
                    });
            }
        }

        private static readonly DateTimeOffset SeededAt =
            new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>Erp.SharedKernel's SystemUsers.Seed. Repeated as a literal because a migration must not drift with code.</summary>
        private static readonly Guid SeedUserId = new("ffffffff-0000-0000-0000-000000000002");

        /// <summary>
        /// Ordered within each list, because these have a natural order that is
        /// neither alphabetical nor arbitrary — a unit list wants NOS first.
        /// </summary>
        private static readonly (string Type, string Code, string Name)[] StartingOptions =
        {
            ("uom", "NOS", "Numbers"), ("uom", "KG", "Kilogram"), ("uom", "MTR", "Metre"),
            ("uom", "LTR", "Litre"), ("uom", "SET", "Set"), ("uom", "BOX", "Box"),
            ("uom", "PKT", "Packet"), ("uom", "ROLL", "Roll"), ("uom", "SQM", "Square metre"),
            ("uom", "HR", "Hour"), ("uom", "PAIR", "Pair"), ("uom", "TON", "Tonne"),

            ("currency", "INR", "Indian rupee"), ("currency", "USD", "US dollar"),
            ("currency", "EUR", "Euro"), ("currency", "GBP", "Pound sterling"),
            ("currency", "JPY", "Japanese yen"), ("currency", "AED", "UAE dirham"),

            ("country", "India", "India"), ("country", "United States", "United States"),
            ("country", "Germany", "Germany"), ("country", "China", "China"),
            ("country", "Japan", "Japan"), ("country", "United Arab Emirates", "United Arab Emirates"),
            ("country", "United Kingdom", "United Kingdom"), ("country", "Italy", "Italy"),

            ("state", "Maharashtra", "Maharashtra (27)"), ("state", "Gujarat", "Gujarat (24)"),
            ("state", "Tamil Nadu", "Tamil Nadu (33)"), ("state", "Karnataka", "Karnataka (29)"),
            ("state", "Telangana", "Telangana (36)"), ("state", "Delhi", "Delhi (07)"),
            ("state", "Haryana", "Haryana (06)"), ("state", "Punjab", "Punjab (03)"),
            ("state", "Uttar Pradesh", "Uttar Pradesh (09)"), ("state", "Rajasthan", "Rajasthan (08)"),
            ("state", "Madhya Pradesh", "Madhya Pradesh (23)"), ("state", "West Bengal", "West Bengal (19)"),
            ("state", "Kerala", "Kerala (32)"), ("state", "Odisha", "Odisha (21)"),
            ("state", "Jharkhand", "Jharkhand (20)"), ("state", "Goa", "Goa (30)"),
            ("state", "Uttarakhand", "Uttarakhand (05)"), ("state", "Bihar", "Bihar (10)"),
            ("state", "Chhattisgarh", "Chhattisgarh (22)"), ("state", "Assam", "Assam (18)"),

            ("part.categoryCode", "RAW", "Raw material"), ("part.categoryCode", "BOUGHT", "Bought out"),
            ("part.categoryCode", "CONSUMABLE", "Consumable"), ("part.categoryCode", "SPARE", "Spare"),
            ("part.categoryCode", "TOOL", "Tool"), ("part.categoryCode", "PACKAGING", "Packaging"),

            ("part.type", "Fabricated", "Fabricated"), ("part.type", "Machined", "Machined"),
            ("part.type", "Standard", "Standard"), ("part.type", "Assembly", "Assembly"),
            ("part.type", "Consumable", "Consumable"),

            ("part.formCategory", "Plate", "Plate"), ("part.formCategory", "Sheet", "Sheet"),
            ("part.formCategory", "Bar", "Bar"), ("part.formCategory", "Pipe", "Pipe"),
            ("part.formCategory", "Shaft", "Shaft"), ("part.formCategory", "Bearing", "Bearing"),
            ("part.formCategory", "Motor", "Motor"), ("part.formCategory", "Sensor", "Sensor"),
            ("part.formCategory", "Cylinder", "Cylinder"), ("part.formCategory", "Valve", "Valve"),
            ("part.formCategory", "Fastener", "Fastener"), ("part.formCategory", "Belt", "Belt"),
            ("part.formCategory", "Gearbox", "Gearbox"), ("part.formCategory", "Assembly", "Assembly"),

            ("part.materialType", "PLT", "Plate"), ("part.materialType", "SHT", "Sheet"),
            ("part.materialType", "BRG", "Bearing"), ("part.materialType", "MTR", "Motor"),
            ("part.materialType", "SEN", "Sensor"), ("part.materialType", "CYL", "Cylinder"),
            ("part.materialType", "VLV", "Valve"), ("part.materialType", "FST", "Fastener"),
            ("part.materialType", "SHF", "Shaft"), ("part.materialType", "GBX", "Gearbox"),
            ("part.materialType", "BLT", "Belt"), ("part.materialType", "FRL", "FRL unit"),

            ("part.seriesCode", "MS", "Mild steel"), ("part.seriesCode", "SS", "Stainless steel"),
            ("part.seriesCode", "EL", "Electrical"), ("part.seriesCode", "HY", "Hydraulic"),
            ("part.seriesCode", "PN", "Pneumatic"), ("part.seriesCode", "FA", "Fasteners"),
            ("part.seriesCode", "MC", "Mechanical"), ("part.seriesCode", "CN", "Conveyor"),
            ("part.seriesCode", "BR", "Bearings"),

            ("part.sourceCode", "In House", "In house"), ("part.sourceCode", "OutSource", "Outsourced"),

            ("part.revisionNo", "00", "00"), ("part.revisionNo", "01", "01"),
            ("part.revisionNo", "02", "02"), ("part.revisionNo", "03", "03"),
            ("part.revisionNo", "04", "04"), ("part.revisionNo", "05", "05"),
            ("part.revisionNo", "06", "06"), ("part.revisionNo", "07", "07"),
            ("part.revisionNo", "08", "08"), ("part.revisionNo", "09", "09"),

            ("moc", "Mild steel", "Mild steel"), ("moc", "SS 304", "SS 304"),
            ("moc", "SS 316L", "SS 316L"), ("moc", "Aluminium", "Aluminium"),
            ("moc", "Cast iron", "Cast iron"), ("moc", "Chrome steel", "Chrome steel"),
            ("moc", "Carbon steel", "Carbon steel"), ("moc", "Spring steel", "Spring steel"),
            ("moc", "EN8", "EN8"), ("moc", "EN19", "EN19"), ("moc", "Brass", "Brass"),
            ("moc", "Copper", "Copper"), ("moc", "Polyurethane", "Polyurethane"),
            ("moc", "Plastic", "Plastic"), ("moc", "Rubber", "Rubber"),

            ("supplier.type", "Local Manufacturer", "Local manufacturer"),
            ("supplier.type", "International Manufacturer", "International manufacturer"),
            ("supplier.type", "Local Distributor", "Local distributor"),
            ("supplier.type", "International Distributor", "International distributor"),
            ("supplier.type", "Job Worker", "Job worker"),
            ("supplier.type", "Service Provider", "Service provider"),

            ("paymentTerms", "Advance 100%", "Advance 100%"),
            ("paymentTerms", "Advance 50% balance on delivery", "Advance 50%, balance on delivery"),
            ("paymentTerms", "Against delivery", "Against delivery"),
            ("paymentTerms", "15 days from invoice", "15 days from invoice"),
            ("paymentTerms", "30 days from invoice", "30 days from invoice"),
            ("paymentTerms", "45 days from invoice", "45 days from invoice"),
            ("paymentTerms", "60 days from GRN", "60 days from GRN"),
            ("paymentTerms", "90 days from invoice", "90 days from invoice"),

            ("taxCode", "GST0", "GST 0%"), ("taxCode", "GST5", "GST 5%"),
            ("taxCode", "GST12", "GST 12%"), ("taxCode", "GST18", "GST 18%"),
            ("taxCode", "GST28", "GST 28%"), ("taxCode", "IGST5", "IGST 5%"),
            ("taxCode", "IGST12", "IGST 12%"), ("taxCode", "IGST18", "IGST 18%"),
            ("taxCode", "IGST28", "IGST 28%"),

            ("customer.industry", "Automotive", "Automotive"),
            ("customer.industry", "Packaging", "Packaging"),
            ("customer.industry", "Food processing", "Food processing"),
            ("customer.industry", "Beverages", "Beverages"),
            ("customer.industry", "Pharmaceutical", "Pharmaceutical"),
            ("customer.industry", "Textile", "Textile"),
            ("customer.industry", "Dairy", "Dairy"),
            ("customer.industry", "Chemicals", "Chemicals"),
            ("customer.industry", "Electrical", "Electrical"),
            ("customer.industry", "Cosmetics", "Cosmetics"),
            ("customer.industry", "Paper", "Paper"),
            ("customer.industry", "Confectionery", "Confectionery"),
            ("customer.industry", "Ceramics", "Ceramics"),
            ("customer.industry", "Edible oil", "Edible oil"),
            ("customer.industry", "Plastics", "Plastics"),
            ("customer.industry", "Spices", "Spices"),
            ("customer.industry", "Marine", "Marine"),
            ("customer.industry", "Steel", "Steel"),
            ("customer.industry", "Cement", "Cement"),
            ("customer.industry", "Seafood", "Seafood"),

            ("employee.gender", "Male", "Male"), ("employee.gender", "Female", "Female"),
            ("employee.gender", "Other", "Other"),

            ("employee.department", "Design", "Design"), ("employee.department", "Production", "Production"),
            ("employee.department", "Quality", "Quality"), ("employee.department", "Stores", "Stores"),
            ("employee.department", "Purchase", "Purchase"), ("employee.department", "Sales", "Sales"),
            ("employee.department", "Accounts", "Accounts"), ("employee.department", "HR", "HR"),
            ("employee.department", "Maintenance", "Maintenance"), ("employee.department", "Dispatch", "Dispatch"),
            ("employee.department", "IT", "IT"), ("employee.department", "Service", "Service"),

            ("employee.designation", "Operator", "Operator"), ("employee.designation", "Technician", "Technician"),
            ("employee.designation", "Assistant", "Assistant"), ("employee.designation", "Engineer", "Engineer"),
            ("employee.designation", "Senior Engineer", "Senior engineer"),
            ("employee.designation", "Executive", "Executive"), ("employee.designation", "Supervisor", "Supervisor"),
            ("employee.designation", "Manager", "Manager"), ("employee.designation", "Head", "Head"),

            ("employee.bloodGroup", "A+", "A+"), ("employee.bloodGroup", "A-", "A-"),
            ("employee.bloodGroup", "B+", "B+"), ("employee.bloodGroup", "B-", "B-"),
            ("employee.bloodGroup", "AB+", "AB+"), ("employee.bloodGroup", "AB-", "AB-"),
            ("employee.bloodGroup", "O+", "O+"), ("employee.bloodGroup", "O-", "O-"),

            ("employee.qualification", "ITI", "ITI"), ("employee.qualification", "Diploma", "Diploma"),
            ("employee.qualification", "BE", "BE"), ("employee.qualification", "BTech", "B.Tech"),
            ("employee.qualification", "ME", "ME"), ("employee.qualification", "MTech", "M.Tech"),
            ("employee.qualification", "BSc", "B.Sc"), ("employee.qualification", "MSc", "M.Sc"),
            ("employee.qualification", "BCom", "B.Com"), ("employee.qualification", "MCom", "M.Com"),
            ("employee.qualification", "MBA", "MBA"), ("employee.qualification", "Other", "Other"),
        };

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LookupValue",
                schema: "masters");
        }
    }
}
