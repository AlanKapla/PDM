using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryHierarchyToProjectFilePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "ProjectFilePackages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ParentId",
                table: "ProjectFilePackages",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [ParentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_ParentId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "ParentId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [ParentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFilePackages_ProjectFilePackages_ParentId",
                table: "ProjectFilePackages",
                column: "ParentId",
                principalTable: "ProjectFilePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_ProjectFilePackages_ParentId",
                table: "ProjectFilePackages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFilePackages_ParentId",
                table: "ProjectFilePackages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_ParentId_Name",
                table: "ProjectFilePackages");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ProjectFilePackages");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
