using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Webhooks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateWebhooksSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "webhooks");

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                schema: "webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SigningSecret = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EventTypesJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId_IsActive",
                schema: "webhooks",
                table: "WebhookSubscriptions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId_IsDeleted",
                schema: "webhooks",
                table: "WebhookSubscriptions",
                columns: new[] { "TenantId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookSubscriptions",
                schema: "webhooks");
        }
    }
}
