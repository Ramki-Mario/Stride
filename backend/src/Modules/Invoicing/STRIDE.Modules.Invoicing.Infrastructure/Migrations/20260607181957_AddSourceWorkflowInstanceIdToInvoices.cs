using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.Invoicing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceWorkflowInstanceIdToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ClientEmail",
                schema: "invoicing",
                table: "Invoices",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceWorkflowInstanceId",
                schema: "invoicing",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_SourceWorkflowInstanceId",
                schema: "invoicing",
                table: "Invoices",
                columns: new[] { "TenantId", "SourceWorkflowInstanceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_SourceWorkflowInstanceId",
                schema: "invoicing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SourceWorkflowInstanceId",
                schema: "invoicing",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "ClientEmail",
                schema: "invoicing",
                table: "Invoices",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320,
                oldNullable: true);
        }
    }
}
