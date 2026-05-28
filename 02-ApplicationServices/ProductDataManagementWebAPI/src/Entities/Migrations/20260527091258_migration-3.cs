using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Roles_RoleId",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_RoleId",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "ProjectMembers");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "ProjectMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProjectMemberModulePermissions",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberModulePermissions", x => new { x.TenantId, x.ProjectId, x.UserId, x.Module });
                    table.ForeignKey(
                        name: "FK_ProjectMemberModulePermissions_ProjectMembers_TenantId_ProjectId_UserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.UserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMemberModulePermissions");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "ProjectMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "ProjectMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_RoleId",
                table: "ProjectMembers",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Roles_RoleId",
                table: "ProjectMembers",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
