using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateTenantSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administration");

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, defaultValue: ""),
                    DefaultPalette = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "purple"),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    CustomCssTokensJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                schema: "administration",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSettings",
                schema: "administration");
        }
    }
}
