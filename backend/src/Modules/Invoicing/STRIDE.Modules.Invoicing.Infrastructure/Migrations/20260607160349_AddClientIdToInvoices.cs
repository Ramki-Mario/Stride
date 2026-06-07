using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Invoicing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                schema: "invoicing",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_ClientId",
                schema: "invoicing",
                table: "Invoices",
                columns: new[] { "TenantId", "ClientId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_ClientId",
                schema: "invoicing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "invoicing",
                table: "Invoices");
        }
    }
}
