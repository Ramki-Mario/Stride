using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.KitOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateKitOpsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kitops");

            migrationBuilder.CreateTable(
                name: "KitItems",
                schema: "kitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitCheckouts",
                schema: "kitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckedOutByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedReturnAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitCheckouts_KitItems_KitItemId",
                        column: x => x.KitItemId,
                        principalSchema: "kitops",
                        principalTable: "KitItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KitReservations",
                schema: "kitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitReservations_KitItems_KitItemId",
                        column: x => x.KitItemId,
                        principalSchema: "kitops",
                        principalTable: "KitItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_KitItemId",
                schema: "kitops",
                table: "KitCheckouts",
                column: "KitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_TenantId_CheckedOutByUserId",
                schema: "kitops",
                table: "KitCheckouts",
                columns: new[] { "TenantId", "CheckedOutByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_TenantId_IsDeleted",
                schema: "kitops",
                table: "KitCheckouts",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_TenantId_KitItemId_Status",
                schema: "kitops",
                table: "KitCheckouts",
                columns: new[] { "TenantId", "KitItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_TenantId_Category",
                schema: "kitops",
                table: "KitItems",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_TenantId_IsActive",
                schema: "kitops",
                table: "KitItems",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_TenantId_IsDeleted",
                schema: "kitops",
                table: "KitItems",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_TenantId_Name",
                schema: "kitops",
                table: "KitItems",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_KitItemId",
                schema: "kitops",
                table: "KitReservations",
                column: "KitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_TenantId_CreatedAt",
                schema: "kitops",
                table: "KitReservations",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_TenantId_IsDeleted",
                schema: "kitops",
                table: "KitReservations",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_TenantId_KitItemId_Status",
                schema: "kitops",
                table: "KitReservations",
                columns: new[] { "TenantId", "KitItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_TenantId_RequestedByUserId",
                schema: "kitops",
                table: "KitReservations",
                columns: new[] { "TenantId", "RequestedByUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitCheckouts",
                schema: "kitops");

            migrationBuilder.DropTable(
                name: "KitReservations",
                schema: "kitops");

            migrationBuilder.DropTable(
                name: "KitItems",
                schema: "kitops");
        }
    }
}
