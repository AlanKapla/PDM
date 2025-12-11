using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationseventh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedProjectCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedProjectCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedProjectCosts_ProjectCosts_ProjectCostId",
                        column: x => x.ProjectCostId,
                        principalTable: "ProjectCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedProjectCosts_TenantMembers_TenantId_SharedByUserId",
                        columns: x => new { x.TenantId, x.SharedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedProjectCosts_TenantMembers_TenantId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.SharedWithUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_ProjectCostId",
                table: "SharedProjectCosts",
                column: "ProjectCostId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_ProjectCostId_SharedWithUserId",
                table: "SharedProjectCosts",
                columns: new[] { "ProjectCostId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_TenantId_ProjectId_SharedWithUserId",
                table: "SharedProjectCosts",
                columns: new[] { "TenantId", "ProjectId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_TenantId_SharedByUserId",
                table: "SharedProjectCosts",
                columns: new[] { "TenantId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_TenantId_SharedWithUserId",
                table: "SharedProjectCosts",
                columns: new[] { "TenantId", "SharedWithUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedProjectCosts");
        }
    }
}
