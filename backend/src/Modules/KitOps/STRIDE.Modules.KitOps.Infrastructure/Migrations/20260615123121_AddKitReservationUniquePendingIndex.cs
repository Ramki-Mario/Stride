using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STRIDE.Modules.KitOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKitReservationUniquePendingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KitReservations_TenantId_KitItemId_RequestedByUserId",
                schema: "kitops",
                table: "KitReservations",
                columns: new[] { "TenantId", "KitItemId", "RequestedByUserId" },
                unique: true,
                filter: "[Status] = 0 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KitReservations_TenantId_KitItemId_RequestedByUserId",
                schema: "kitops",
                table: "KitReservations");
        }
    }
}
