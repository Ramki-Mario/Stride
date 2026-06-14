using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace STRIDE.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionCatalogAndTenantAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permissions_TenantId_IsDeleted",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_TenantId_Name",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Resource",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "identity",
                table: "Permissions",
                newName: "Key");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                schema: "identity",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "identity",
                table: "Permissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Id", "Description", "Key" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000000"), "View workflow definitions", "workflow.view" },
                    { new Guid("00000002-0000-0000-0000-000000000000"), "Create and edit workflow definitions", "workflow.create" },
                    { new Guid("00000003-0000-0000-0000-000000000000"), "Activate or archive workflows", "workflow.activate" },
                    { new Guid("00000004-0000-0000-0000-000000000000"), "Start a workflow instance", "workflow.run" },
                    { new Guid("00000005-0000-0000-0000-000000000000"), "View and cancel any workflow instance", "workflow.manage_instances" },
                    { new Guid("00000006-0000-0000-0000-000000000000"), "Invite new users to the tenant", "user.invite" },
                    { new Guid("00000007-0000-0000-0000-000000000000"), "Edit user roles and deactivate accounts", "user.manage" },
                    { new Guid("00000008-0000-0000-0000-000000000000"), "View the tenant role catalog", "role.view" },
                    { new Guid("00000009-0000-0000-0000-000000000000"), "Create, edit, and delete tenant roles", "role.manage" },
                    { new Guid("0000000a-0000-0000-0000-000000000000"), "Edit tenant-level settings", "tenant.settings" }
                });

            // ── Migrate existing tenants: create TenantAdmin system role + assign permissions + assign to existing users ──
            migrationBuilder.Sql(@"
-- 1. Create TenantAdmin system role for each existing tenant (idempotent)
INSERT INTO [identity].[Roles]
    (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
SELECT
    NEWID(), t.Id, 'TenantAdmin', 'TENANTADMIN',
    'Full system administrator — all permissions (system role, cannot be deleted)',
    1, GETUTCDATE(), GETUTCDATE(), '00000000-0000-0000-0000-000000000000', 0
FROM [identity].[Tenants] t
WHERE NOT EXISTS (
    SELECT 1 FROM [identity].[Roles] r
    WHERE r.TenantId = t.Id AND r.NormalizedName = 'TENANTADMIN' AND r.IsDeleted = 0
);

-- 2. Grant all 10 permissions to each TenantAdmin role (idempotent)
INSERT INTO [identity].[RolePermissions]
    (Id, TenantId, RoleId, PermissionId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
SELECT
    NEWID(), r.TenantId, r.Id, p.Id,
    GETUTCDATE(), GETUTCDATE(), '00000000-0000-0000-0000-000000000000', 0
FROM [identity].[Roles] r
CROSS JOIN [identity].[Permissions] p
WHERE r.NormalizedName = 'TENANTADMIN' AND r.IsDeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM [identity].[RolePermissions] rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id AND rp.IsDeleted = 0
);

-- 3. Assign TenantAdmin role to users who have any existing role in their tenant (idempotent)
INSERT INTO [identity].[UserRoles]
    (Id, TenantId, UserId, RoleId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
SELECT DISTINCT
    NEWID(), ta.TenantId, ur.UserId, ta.Id,
    GETUTCDATE(), GETUTCDATE(), '00000000-0000-0000-0000-000000000000', 0
FROM [identity].[UserRoles] ur
INNER JOIN [identity].[Roles] ta
    ON ta.TenantId = ur.TenantId AND ta.NormalizedName = 'TENANTADMIN' AND ta.IsDeleted = 0
WHERE ur.IsDeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM [identity].[UserRoles] ex
    WHERE ex.UserId = ur.UserId AND ex.RoleId = ta.Id AND ex.IsDeleted = 0
);
");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                schema: "identity",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId",
                principalSchema: "identity",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Key",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "Key",
                schema: "identity",
                table: "Permissions",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                schema: "identity",
                table: "Permissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "identity",
                table: "Permissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "identity",
                table: "Permissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Resource",
                schema: "identity",
                table: "Permissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "identity",
                table: "Permissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "Permissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_TenantId_IsDeleted",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_TenantId_Name",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }
    }
}
