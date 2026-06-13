using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedWorkflowLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedWorkflowLinks",
                schema: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedWorkflowLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedWorkflowLinks_Token",
                schema: "workflows",
                table: "SharedWorkflowLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedWorkflowLinks_WorkflowInstanceId_TenantId",
                schema: "workflows",
                table: "SharedWorkflowLinks",
                columns: new[] { "WorkflowInstanceId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedWorkflowLinks",
                schema: "workflows");
        }
    }
}
