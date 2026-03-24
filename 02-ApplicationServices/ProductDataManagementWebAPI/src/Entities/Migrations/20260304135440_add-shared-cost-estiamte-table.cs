using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addsharedcostestiamtetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedCostEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedCostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_ProjectMembers_TenantId_ProjectId_SharedByUserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.SharedByUserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_ProjectMembers_TenantId_ProjectId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.SharedWithUserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_TenantMembers_TenantId_SharedByUserId",
                        columns: x => new { x.TenantId, x.SharedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_TenantMembers_TenantId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.SharedWithUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_Users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_CostEstimateId",
                table: "SharedCostEstimates",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_CostEstimateId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "CostEstimateId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_SharedByUserId",
                table: "SharedCostEstimates",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_SharedWithUserId_ProjectId",
                table: "SharedCostEstimates",
                columns: new[] { "SharedWithUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_ProjectId_SharedByUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "ProjectId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_ProjectId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "ProjectId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_SharedByUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "SharedWithUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedCostEstimates");
        }
    }
}
