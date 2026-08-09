using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Modules.Masters.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssemblyNodeAndParentPartMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssemblyNode",
                schema: "masters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ManualCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MachineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DrivenBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DrawingPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TechnicalSpecification = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DisplaySequence = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BusinessUnitId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AssemblyNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssemblyNode_AssemblyNode_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "masters",
                        principalTable: "AssemblyNode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParentPart",
                schema: "masters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AssemblyNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DrawingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BusinessUnitId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ParentPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPart_AssemblyNode_AssemblyNodeId",
                        column: x => x.AssemblyNodeId,
                        principalSchema: "masters",
                        principalTable: "AssemblyNode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentPart_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "masters",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParentPartComponent",
                schema: "masters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentPartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitOfMeasureCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UnitWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LineWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DrawingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPartComponent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPartComponent_ParentPart_ParentPartId",
                        column: x => x.ParentPartId,
                        principalSchema: "masters",
                        principalTable: "ParentPart",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentPartComponent_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "masters",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyNode_BusinessUnit_Level_Code",
                schema: "masters",
                table: "AssemblyNode",
                columns: new[] { "BusinessUnitId", "Level", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyNode_BusinessUnit_Parent",
                schema: "masters",
                table: "AssemblyNode",
                columns: new[] { "BusinessUnitId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyNode_IsDeleted",
                schema: "masters",
                table: "AssemblyNode",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyNode_ParentId",
                schema: "masters",
                table: "AssemblyNode",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_AssemblyNode_BusinessUnit_Code",
                schema: "masters",
                table: "AssemblyNode",
                columns: new[] { "BusinessUnitId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AssemblyNode_BusinessUnit_Level_Parent_Name",
                schema: "masters",
                table: "AssemblyNode",
                columns: new[] { "BusinessUnitId", "Level", "ParentId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPart_AssemblyNodeId",
                schema: "masters",
                table: "ParentPart",
                column: "AssemblyNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPart_BusinessUnit_AssemblyNode",
                schema: "masters",
                table: "ParentPart",
                columns: new[] { "BusinessUnitId", "AssemblyNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPart_IsDeleted",
                schema: "masters",
                table: "ParentPart",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPart_PartId",
                schema: "masters",
                table: "ParentPart",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "UX_ParentPart_BusinessUnit_Part",
                schema: "masters",
                table: "ParentPart",
                columns: new[] { "BusinessUnitId", "PartId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPartComponent_ParentPart_LineNumber",
                schema: "masters",
                table: "ParentPartComponent",
                columns: new[] { "ParentPartId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPartComponent_PartId",
                schema: "masters",
                table: "ParentPartComponent",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "UX_ParentPartComponent_ParentPart_Part",
                schema: "masters",
                table: "ParentPartComponent",
                columns: new[] { "ParentPartId", "PartId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentPartComponent",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "ParentPart",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "AssemblyNode",
                schema: "masters");
        }
    }
}
