using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalGates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionHandling",
                schema: "workflows",
                table: "StepInstances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "HaltWorkflow");

            migrationBuilder.AddColumn<int>(
                name: "RevertToStepOrder",
                schema: "workflows",
                table: "StepInstances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                schema: "workflows",
                table: "StepInstances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "RejectionHandling",
                schema: "workflows",
                table: "StepDefinitions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "HaltWorkflow");

            migrationBuilder.AddColumn<int>(
                name: "RevertToStepOrder",
                schema: "workflows",
                table: "StepDefinitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                schema: "workflows",
                table: "StepDefinitions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedFromRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionHandling = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "HaltWorkflow"),
                    RevertToStepOrder = table.Column<int>(type: "int", nullable: true),
                    DecisionByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "workflows",
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_StepInstanceId_Status",
                schema: "workflows",
                table: "ApprovalRequests",
                columns: new[] { "StepInstanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_TenantId_WorkflowInstanceId",
                schema: "workflows",
                table: "ApprovalRequests",
                columns: new[] { "TenantId", "WorkflowInstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowInstanceId",
                schema: "workflows",
                table: "ApprovalRequests",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "workflows");

            migrationBuilder.DropColumn(
                name: "RejectionHandling",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "RevertToStepOrder",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "StepType",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "RejectionHandling",
                schema: "workflows",
                table: "StepDefinitions");

            migrationBuilder.DropColumn(
                name: "RevertToStepOrder",
                schema: "workflows",
                table: "StepDefinitions");

            migrationBuilder.DropColumn(
                name: "StepType",
                schema: "workflows",
                table: "StepDefinitions");
        }
    }
}
