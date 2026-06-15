using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnabledModulesToTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnabledModules",
                schema: "administration",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                defaultValue: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledModules",
                schema: "administration",
                table: "TenantSettings");
        }
    }
}
