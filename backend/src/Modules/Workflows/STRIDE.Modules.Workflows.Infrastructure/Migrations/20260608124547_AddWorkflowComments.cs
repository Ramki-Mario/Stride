using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowComments",
                schema: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowComments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowComments_TenantId_IsDeleted",
                schema: "workflows",
                table: "WorkflowComments",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowComments_WorkflowInstanceId_TenantId_CreatedAt",
                schema: "workflows",
                table: "WorkflowComments",
                columns: new[] { "WorkflowInstanceId", "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowComments",
                schema: "workflows");
        }
    }
}
