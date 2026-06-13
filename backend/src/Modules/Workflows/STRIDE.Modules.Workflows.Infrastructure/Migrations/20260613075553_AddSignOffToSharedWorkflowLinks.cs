using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignOffToSharedWorkflowLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SignedOffAt",
                schema: "workflows",
                table: "SharedWorkflowLinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedOffBy",
                schema: "workflows",
                table: "SharedWorkflowLinks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedOffAt",
                schema: "workflows",
                table: "SharedWorkflowLinks");

            migrationBuilder.DropColumn(
                name: "SignedOffBy",
                schema: "workflows",
                table: "SharedWorkflowLinks");
        }
    }
}
