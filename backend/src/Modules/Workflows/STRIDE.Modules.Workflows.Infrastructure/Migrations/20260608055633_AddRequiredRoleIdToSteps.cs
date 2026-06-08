using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredRoleIdToSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                schema: "workflows",
                table: "StepInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequiredRoleId",
                schema: "workflows",
                table: "StepInstances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequiredRoleId",
                schema: "workflows",
                table: "StepDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepDefinitions_RequiredRoleId",
                schema: "workflows",
                table: "StepDefinitions",
                column: "RequiredRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StepDefinitions_RequiredRoleId",
                schema: "workflows",
                table: "StepDefinitions");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "RequiredRoleId",
                schema: "workflows",
                table: "StepInstances");

            migrationBuilder.DropColumn(
                name: "RequiredRoleId",
                schema: "workflows",
                table: "StepDefinitions");
        }
    }
}
