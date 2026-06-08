using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStepFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StepFieldValues",
                schema: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepFieldValues_StepFieldDefinitions_StepFieldDefinitionId",
                        column: x => x.StepFieldDefinitionId,
                        principalSchema: "workflows",
                        principalTable: "StepFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StepFieldValues_StepInstances_StepInstanceId",
                        column: x => x.StepInstanceId,
                        principalSchema: "workflows",
                        principalTable: "StepInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepFieldValues_StepFieldDefinitionId",
                schema: "workflows",
                table: "StepFieldValues",
                column: "StepFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StepFieldValues_StepInstanceId",
                schema: "workflows",
                table: "StepFieldValues",
                column: "StepInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StepFieldValues_TenantId_StepInstanceId",
                schema: "workflows",
                table: "StepFieldValues",
                columns: new[] { "TenantId", "StepInstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StepFieldValues_TenantId_StepInstanceId_StepFieldDefinitionId",
                schema: "workflows",
                table: "StepFieldValues",
                columns: new[] { "TenantId", "StepInstanceId", "StepFieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StepFieldValues",
                schema: "workflows");
        }
    }
}
