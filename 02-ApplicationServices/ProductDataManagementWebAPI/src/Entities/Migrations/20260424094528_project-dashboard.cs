using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class projectdashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimates_CostTrackers_CostTrackerId",
                table: "CostEstimates");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_CostTrackers_CostTrackerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_CostTrackers_TrackerId",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_CostEstimates_CostEstimateId",
                table: "WorkSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStages_CostEstimateGroups_CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropTable(
                name: "CostTrackers");

            migrationBuilder.DropTable(
                name: "ProjectCostTrackedCostLinks");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStages_CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_CostEstimateId",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_TrackedCosts_TrackerId",
                table: "TrackedCosts");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CostTrackerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimates_CostTrackerId",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "CostEstimateId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CostTrackerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CostTrackerId",
                table: "CostEstimates");

            migrationBuilder.RenameColumn(
                name: "TrackerId",
                table: "TrackedCosts",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "CostEstimateId",
                table: "TrackedCosts",
                newName: "WorkScheduleStageWorkId");

            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateItemId1",
                table: "TrackedCosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "TrackedCosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TrackedCosts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkItemLinkId",
                table: "TrackedCosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetGross",
                table: "Projects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetNet",
                table: "Projects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostEstimateWorkScheduleLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateWorkScheduleLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateWorkScheduleLinks_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateWorkScheduleLinks_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateGroupWorkScheduleStageLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkScheduleStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateGroupWorkScheduleStageLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroupWorkScheduleStageLinks_CostEstimateGroups_CostEstimateGroupId",
                        column: x => x.CostEstimateGroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroupWorkScheduleStageLinks_CostEstimateWorkScheduleLinks_WorkScheduleLinkId",
                        column: x => x.WorkScheduleLinkId,
                        principalTable: "CostEstimateWorkScheduleLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroupWorkScheduleStageLinks_WorkScheduleStages_WorkScheduleStageId",
                        column: x => x.WorkScheduleStageId,
                        principalTable: "WorkScheduleStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItemWorkScheduleStageWorkLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupStageLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostEstimateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BudgetNet = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BudgetGross = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsWorkClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItemWorkScheduleStageWorkLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemWorkScheduleStageWorkLinks_CostEstimateGroupWorkScheduleStageLinks_GroupStageLinkId",
                        column: x => x.GroupStageLinkId,
                        principalTable: "CostEstimateGroupWorkScheduleStageLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemWorkScheduleStageWorkLinks_CostEstimateItems_CostEstimateItemId",
                        column: x => x.CostEstimateItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemWorkScheduleStageWorkLinks_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_ProjectId",
                table: "WorkScheduleStageWorkAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_TenantId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_CostEstimateItemId1",
                table: "TrackedCosts",
                column: "CostEstimateItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_TenantId_ProjectId",
                table: "TrackedCosts",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_WorkItemLinkId",
                table: "TrackedCosts",
                column: "WorkItemLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_WorkScheduleStageWorkId",
                table: "TrackedCosts",
                column: "WorkScheduleStageWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupWorkScheduleStageLinks_CostEstimateGroupId",
                table: "CostEstimateGroupWorkScheduleStageLinks",
                column: "CostEstimateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupWorkScheduleStageLinks_WorkScheduleLinkId_CostEstimateGroupId",
                table: "CostEstimateGroupWorkScheduleStageLinks",
                columns: new[] { "WorkScheduleLinkId", "CostEstimateGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupWorkScheduleStageLinks_WorkScheduleLinkId_WorkScheduleStageId",
                table: "CostEstimateGroupWorkScheduleStageLinks",
                columns: new[] { "WorkScheduleLinkId", "WorkScheduleStageId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupWorkScheduleStageLinks_WorkScheduleStageId",
                table: "CostEstimateGroupWorkScheduleStageLinks",
                column: "WorkScheduleStageId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemWorkScheduleStageWorkLinks_CostEstimateItemId",
                table: "CostEstimateItemWorkScheduleStageWorkLinks",
                column: "CostEstimateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemWorkScheduleStageWorkLinks_GroupStageLinkId",
                table: "CostEstimateItemWorkScheduleStageWorkLinks",
                column: "GroupStageLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemWorkScheduleStageWorkLinks_ProjectId_CostEstimateItemId",
                table: "CostEstimateItemWorkScheduleStageWorkLinks",
                columns: new[] { "ProjectId", "CostEstimateItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemWorkScheduleStageWorkLinks_ProjectId_WorkScheduleStageWorkId",
                table: "CostEstimateItemWorkScheduleStageWorkLinks",
                columns: new[] { "ProjectId", "WorkScheduleStageWorkId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemWorkScheduleStageWorkLinks_WorkScheduleStageWorkId",
                table: "CostEstimateItemWorkScheduleStageWorkLinks",
                column: "WorkScheduleStageWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateWorkScheduleLinks_CostEstimateId",
                table: "CostEstimateWorkScheduleLinks",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateWorkScheduleLinks_CostEstimateId_WorkScheduleId",
                table: "CostEstimateWorkScheduleLinks",
                columns: new[] { "CostEstimateId", "WorkScheduleId" },
                unique: true,
                filter: "[CostEstimateId] IS NOT NULL AND [WorkScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateWorkScheduleLinks_WorkScheduleId",
                table: "CostEstimateWorkScheduleLinks",
                column: "WorkScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_CostEstimateItemWorkScheduleStageWorkLinks_WorkItemLinkId",
                table: "TrackedCosts",
                column: "WorkItemLinkId",
                principalTable: "CostEstimateItemWorkScheduleStageWorkLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId",
                table: "TrackedCosts",
                column: "CostEstimateItemId",
                principalTable: "CostEstimateItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId1",
                table: "TrackedCosts",
                column: "CostEstimateItemId1",
                principalTable: "CostEstimateItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                table: "TrackedCosts",
                column: "WorkScheduleStageWorkId",
                principalTable: "WorkScheduleStageWorks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Projects_ProjectId",
                table: "WorkScheduleStageWorkAssignments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_TenantMembers_TenantId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Tenants_TenantId",
                table: "WorkScheduleStageWorkAssignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_CostEstimateItemWorkScheduleStageWorkLinks_WorkItemLinkId",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId1",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedCosts_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                table: "TrackedCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Projects_ProjectId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_TenantMembers_TenantId_UserId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Tenants_TenantId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropTable(
                name: "CostEstimateItemWorkScheduleStageWorkLinks");

            migrationBuilder.DropTable(
                name: "CostEstimateGroupWorkScheduleStageLinks");

            migrationBuilder.DropTable(
                name: "CostEstimateWorkScheduleLinks");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkAssignments_ProjectId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkAssignments_TenantId_UserId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TrackedCosts_CostEstimateItemId1",
                table: "TrackedCosts");

            migrationBuilder.DropIndex(
                name: "IX_TrackedCosts_TenantId_ProjectId",
                table: "TrackedCosts");

            migrationBuilder.DropIndex(
                name: "IX_TrackedCosts_WorkItemLinkId",
                table: "TrackedCosts");

            migrationBuilder.DropIndex(
                name: "IX_TrackedCosts_WorkScheduleStageWorkId",
                table: "TrackedCosts");

            migrationBuilder.DropColumn(
                name: "CostEstimateItemId1",
                table: "TrackedCosts");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "TrackedCosts");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TrackedCosts");

            migrationBuilder.DropColumn(
                name: "WorkItemLinkId",
                table: "TrackedCosts");

            migrationBuilder.DropColumn(
                name: "BudgetGross",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BudgetNet",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "WorkScheduleStageWorkId",
                table: "TrackedCosts",
                newName: "CostEstimateId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "TrackedCosts",
                newName: "TrackerId");

            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateGroupId",
                table: "WorkScheduleStages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateId",
                table: "WorkSchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostTrackerId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CostTrackerId",
                table: "CostEstimates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BudgetGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCostTrackedCostLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ProjectCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCostTrackedCostLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCostTrackedCostLinks_ProjectCosts_ProjectCostId",
                        column: x => x.ProjectCostId,
                        principalTable: "ProjectCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCostTrackedCostLinks_TrackedCosts_TrackedCostId",
                        column: x => x.TrackedCostId,
                        principalTable: "TrackedCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_CostEstimateGroupId",
                table: "WorkScheduleStages",
                column: "CostEstimateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_CostEstimateId",
                table: "WorkSchedules",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_TrackerId",
                table: "TrackedCosts",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CostTrackerId",
                table: "Projects",
                column: "CostTrackerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_CostTrackerId",
                table: "CostEstimates",
                column: "CostTrackerId",
                unique: true,
                filter: "[CostTrackerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackers_ProjectId",
                table: "CostTrackers",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackers_TenantId",
                table: "CostTrackers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCostTrackedCostLinks_ProjectCostId",
                table: "ProjectCostTrackedCostLinks",
                column: "ProjectCostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCostTrackedCostLinks_TrackedCostId",
                table: "ProjectCostTrackedCostLinks",
                column: "TrackedCostId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimates_CostTrackers_CostTrackerId",
                table: "CostEstimates",
                column: "CostTrackerId",
                principalTable: "CostTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_CostTrackers_CostTrackerId",
                table: "Projects",
                column: "CostTrackerId",
                principalTable: "CostTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId",
                table: "TrackedCosts",
                column: "CostEstimateItemId",
                principalTable: "CostEstimateItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedCosts_CostTrackers_TrackerId",
                table: "TrackedCosts",
                column: "TrackerId",
                principalTable: "CostTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_CostEstimates_CostEstimateId",
                table: "WorkSchedules",
                column: "CostEstimateId",
                principalTable: "CostEstimates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStages_CostEstimateGroups_CostEstimateGroupId",
                table: "WorkScheduleStages",
                column: "CostEstimateGroupId",
                principalTable: "CostEstimateGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
