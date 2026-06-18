using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuperAdmins",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperAdmins", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdmins_NormalizedEmail",
                schema: "identity",
                table: "SuperAdmins",
                column: "NormalizedEmail",
                unique: true);

            // Seed the sole platform super-admin.
            migrationBuilder.InsertData(
                schema: "identity",
                table: "SuperAdmins",
                columns: ["Id", "NormalizedEmail", "AddedAt"],
                values: [new Guid("00000000-0000-0000-0000-000000000001"), "RAMKI@STRYDESUITE.COM", new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc)]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuperAdmins",
                schema: "identity");
        }
    }
}
