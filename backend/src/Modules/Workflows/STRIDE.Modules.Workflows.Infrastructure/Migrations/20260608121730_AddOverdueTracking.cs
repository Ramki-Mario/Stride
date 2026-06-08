using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOverdueTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSlaBreached",
                schema: "workflows",
                table: "WorkflowInstances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaBreachedNotifiedAt",
                schema: "workflows",
                table: "WorkflowInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverdue",
                schema: "workflows",
                table: "StepInstances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueNotifiedAt",
                schema: "workflows",
                table: "StepInstances",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSlaBreached",
                schema: "workflows",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "SlaBreachedNotifiedAt",
                schema: "workflows",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "IsOverdue",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "OverdueNotifiedAt",
                schema: "workflows",
                table: "StepInstances");
        }
    }
}
