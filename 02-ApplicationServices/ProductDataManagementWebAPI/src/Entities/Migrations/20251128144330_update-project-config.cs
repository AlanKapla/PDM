using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class updateprojectconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_TenantMembers_ProjectId_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_TenantId_UserId",
                table: "ProjectMembers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_TenantMembers_TenantId_UserId",
                table: "ProjectMembers",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_TenantMembers_TenantId_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_TenantId_UserId",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_TenantMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
