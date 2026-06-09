using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamIdToWorkflowInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                schema: "workflows",
                table: "WorkflowInstances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_TenantId_TeamId",
                schema: "workflows",
                table: "WorkflowInstances",
                columns: new[] { "TenantId", "TeamId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstances_TenantId_TeamId",
                schema: "workflows",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "TeamId",
                schema: "workflows",
                table: "WorkflowInstances");
        }
    }
}
