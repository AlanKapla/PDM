using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationcostapprovalstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedProjectCosts");

            migrationBuilder.DropIndex(
                name: "IX_Costs_TenantId_ProjectId_IsAccepted",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "Costs");

            migrationBuilder.RenameColumn(
                name: "AcceptedByUserId",
                table: "Costs",
                newName: "ApprovedByUserId");

            migrationBuilder.RenameColumn(
                name: "AcceptedAt",
                table: "Costs",
                newName: "ApprovedAt");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Costs",
                type: "nvarchar(450)",
                nullable: true,
                defaultValue: "Draft");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId_ApprovalStatus",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "ApprovalStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Costs_TenantId_ProjectId_ApprovalStatus",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Costs");

            migrationBuilder.RenameColumn(
                name: "ApprovedByUserId",
                table: "Costs",
                newName: "AcceptedByUserId");

            migrationBuilder.RenameColumn(
                name: "ApprovedAt",
                table: "Costs",
                newName: "AcceptedAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "Costs",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SharedProjectCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedProjectCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedProjectCosts_Costs_ProjectCostId",
                        column: x => x.ProjectCostId,
                        principalTable: "Costs",
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
                name: "IX_Costs_TenantId_ProjectId_IsAccepted",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "IsAccepted" });

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
    }
}
