using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Api.Persistence.Migrations
{
    /// <summary>
    /// Fills the two new masters, and takes units of measure out of
    /// <c>LookupValue</c> so there is one place a unit is defined rather than two
    /// that can disagree.
    /// </summary>
    /// <inheritdoc />
    public partial class SeedUnitsOfMeasureAndHsnCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedUnits(migrationBuilder);
            SeedHsnCodes(migrationBuilder);
            AddMaterialsTheSampleWorkbookUses(migrationBuilder);

            // The uom rows move rather than being copied. Two tables answering "what
            // units exist?" is how the legacy system ended up with a category master
            // and a hard-coded JavaScript array that disagreed.
            migrationBuilder.Sql("DELETE FROM masters.LookupValue WHERE Type = 'uom';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM masters.HsnGstRate;");
            migrationBuilder.Sql("DELETE FROM masters.HsnCode;");
            migrationBuilder.Sql("DELETE FROM masters.UnitOfMeasure;");

            // Put the units back where they were, or rolling this back leaves every
            // unit dropdown in the application empty.
            var order = 0;

            foreach (var (code, name, _, _, _) in StartingUnits)
            {
                order++;

                migrationBuilder.InsertData(
                    schema: "masters",
                    table: "LookupValue",
                    columns: LookupValueColumns,
                    values: new object[] { "uom", code, name, order, true, SeededAt, SeedUserId, false });
            }

            migrationBuilder.Sql(
                "DELETE FROM masters.LookupValue WHERE Type = 'moc' AND Code IN ('SS 303', 'Steel');");
        }

        private static void SeedUnits(MigrationBuilder migrationBuilder)
        {
            var order = 0;

            foreach (var (code, name, decimals, baseUnitCode, conversionToBase) in StartingUnits)
            {
                order++;

                migrationBuilder.InsertData(
                    schema: "masters",
                    table: "UnitOfMeasure",
                    columns: new[]
                    {
                        "Code", "Name", "Decimals", "BaseUnitCode", "ConversionToBase",
                        "SortOrder", "IsActive", "CreatedAtUtc", "CreatedByUserId", "IsDeleted",
                    },
                    values: new object[]
                    {
                        code, name, decimals, baseUnitCode!, conversionToBase!,
                        order, true, SeededAt, SeedUserId, false,
                    });
            }
        }

        private static void SeedHsnCodes(MigrationBuilder migrationBuilder)
        {
            foreach (var (code, description) in StartingHsnCodes)
            {
                migrationBuilder.InsertData(
                    schema: "masters",
                    table: "HsnCode",
                    columns: new[]
                    {
                        "Code", "Description", "IsActive",
                        "CreatedAtUtc", "CreatedByUserId", "IsDeleted",
                    },
                    values: new object[] { code, description, true, SeededAt, SeedUserId, false });
            }

            // One statement because every code seeded here is an 18% industrial good
            // â€” chapters 72, 73, 84 and 85. That is a fact about this starting set,
            // not about the table: rates are per code and per date, and the next one
            // added may well be 5% or 28%.
            //
            // 1 July 2017 is the date GST came into force. Confirm the rates against
            // the current CBIC schedule before they reach an invoice; they are seeded
            // to make the master usable, not to be a tax authority.
            migrationBuilder.Sql(
                """
                INSERT INTO masters.HsnGstRate (HsnCodeId, RatePercent, EffectiveFrom)
                SELECT Id, 18.00, '2017-07-01' FROM masters.HsnCode;
                """);
        }

        /// <summary>
        /// Two materials the sample import workbook uses that the original seed did
        /// not have. Without them, turning on the code check would make
        /// <c>docs/import/Parts.xlsx</c> â€” the file the documentation tells an
        /// operator to start from â€” fail on its own data.
        /// </summary>
        private static void AddMaterialsTheSampleWorkbookUses(MigrationBuilder migrationBuilder)
        {
            var order = 100;

            foreach (var (code, name) in MaterialsAddedHere)
            {
                order++;

                migrationBuilder.InsertData(
                    schema: "masters",
                    table: "LookupValue",
                    columns: LookupValueColumns,
                    values: new object[] { "moc", code, name, order, true, SeededAt, SeedUserId, false });
            }
        }

        private static readonly string[] LookupValueColumns =
        {
            "Type", "Code", "Name", "SortOrder", "IsActive",
            "CreatedAtUtc", "CreatedByUserId", "IsDeleted",
        };

        private static readonly DateTimeOffset SeededAt =
            new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>Erp.Api.Common's SystemUsers.Seed. Repeated as a literal because a migration must not drift with code.</summary>
        private static readonly Guid SeedUserId = new("ffffffff-0000-0000-0000-000000000002");

        /// <summary>
        /// The twelve units that were <c>LookupValue</c> rows, now with the two things
        /// a row could not hold.
        /// <para>
        /// Decimals: zero for anything counted, so 2.5 bearings is rejected where
        /// 2.5 kg is not.
        /// </para>
        /// <para>
        /// Conversion: only TON to KG, because that is the only pair among these that
        /// converts the same way for every part. BOX and PKT deliberately have none â€”
        /// a box of 12 for one part and 50 for another is a fact about the part, and
        /// inventing a global factor for it would be worse than having none.
        /// </para>
        /// </summary>
        private static readonly (string Code, string Name, int Decimals, string BaseUnitCode, decimal? ConversionToBase)[]
            StartingUnits =
            {
                ("NOS", "Numbers", 0, null, null),
                ("KG", "Kilogram", 3, null, null),
                ("MTR", "Metre", 3, null, null),
                ("LTR", "Litre", 3, null, null),
                ("SET", "Set", 0, null, null),
                ("BOX", "Box", 0, null, null),
                ("PKT", "Packet", 0, null, null),
                ("ROLL", "Roll", 0, null, null),
                ("SQM", "Square metre", 3, null, null),
                ("HR", "Hour", 2, null, null),
                ("PAIR", "Pair", 0, null, null),
                ("TON", "Tonne", 3, "KG", 1000m),
            };

        /// <summary>
        /// The codes the sample parts workbook actually uses. A starting set, not the
        /// GST schedule: the master is filled from the codes in use, and grows as
        /// parts are added.
        /// </summary>
        private static readonly (string Code, string Description)[] StartingHsnCodes =
        {
            ("72085190", "Hot rolled steel plate, thickness over 10 mm"),
            ("72193390", "Cold rolled stainless steel sheet, 1 mm to 3 mm"),
            ("73181500", "Screws and bolts, threaded"),
            ("73181600", "Nuts, threaded"),
            ("73182200", "Washers, non-threaded"),
            ("84122010", "Hydraulic cylinders"),
            ("84123100", "Pneumatic cylinders, linear acting"),
            ("84811000", "Pressure reducing valves"),
            ("84812000", "Valves for oleohydraulic and pneumatic transmission"),
            ("84821011", "Ball bearings, radial"),
            ("84822000", "Tapered roller bearings"),
            ("84831099", "Transmission shafts and cranks"),
            ("85015110", "AC motors, multi-phase, output up to 750 W"),
            ("85015210", "AC motors, multi-phase, output over 750 W up to 75 kW"),
            ("85365090", "Switches for a voltage not exceeding 1000 V"),
        };

        private static readonly (string Code, string Name)[] MaterialsAddedHere =
        {
            ("SS 303", "SS 303"),
            ("Steel", "Steel"),
        };
    }
}
