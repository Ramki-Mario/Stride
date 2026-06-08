using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDueDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                schema: "workflows",
                table: "StepInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DueOffsetHours",
                schema: "workflows",
                table: "StepInstances",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DueOffsetHours",
                schema: "workflows",
                table: "StepDefinitions",
                type: "decimal(6,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueAt",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "DueOffsetHours",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "DueOffsetHours",
                schema: "workflows",
                table: "StepDefinitions");
        }
    }
}
