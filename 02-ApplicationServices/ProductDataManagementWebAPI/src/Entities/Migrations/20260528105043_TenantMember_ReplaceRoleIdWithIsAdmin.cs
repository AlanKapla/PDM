using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class TenantMember_ReplaceRoleIdWithIsAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_Roles_RoleId",
                table: "TenantMembers");

            migrationBuilder.DropIndex(
                name: "IX_TenantMembers_RoleId",
                table: "TenantMembers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "TenantMembers");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "TenantMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "TenantMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "TenantMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_RoleId",
                table: "TenantMembers",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMembers_Roles_RoleId",
                table: "TenantMembers",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
