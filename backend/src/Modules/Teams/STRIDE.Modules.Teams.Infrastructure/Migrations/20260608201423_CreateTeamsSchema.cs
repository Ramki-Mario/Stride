using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Teams.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateTeamsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "teams");

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Teams_ParentTeamId",
                        column: x => x.ParentTeamId,
                        principalSchema: "teams",
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ParentTeamId",
                schema: "teams",
                table: "Teams",
                column: "ParentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId_CreatedAt",
                schema: "teams",
                table: "Teams",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId_IsDeleted",
                schema: "teams",
                table: "Teams",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId_Name",
                schema: "teams",
                table: "Teams",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId_Status",
                schema: "teams",
                table: "Teams",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Teams",
                schema: "teams");
        }
    }
}
