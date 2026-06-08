using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineAt",
                schema: "workflows",
                table: "WorkflowInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SlaOffsetHours",
                schema: "workflows",
                table: "WorkflowDefinitions",
                type: "decimal(6,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadlineAt",
                schema: "workflows",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "SlaOffsetHours",
                schema: "workflows",
                table: "WorkflowDefinitions");
        }
    }
}
