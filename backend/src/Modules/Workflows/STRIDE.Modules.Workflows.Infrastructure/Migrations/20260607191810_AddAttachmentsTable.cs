using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Workflows.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_StepInstances_StepInstanceId",
                        column: x => x.StepInstanceId,
                        principalSchema: "workflows",
                        principalTable: "StepInstances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "workflows",
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_StepInstanceId",
                schema: "workflows",
                table: "Attachments",
                column: "StepInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_StorageKey",
                schema: "workflows",
                table: "Attachments",
                column: "StorageKey");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TenantId_StepInstanceId",
                schema: "workflows",
                table: "Attachments",
                columns: new[] { "TenantId", "StepInstanceId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TenantId_WorkflowInstanceId",
                schema: "workflows",
                table: "Attachments",
                columns: new[] { "TenantId", "WorkflowInstanceId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_WorkflowInstanceId",
                schema: "workflows",
                table: "Attachments",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "workflows");
        }
    }
}
