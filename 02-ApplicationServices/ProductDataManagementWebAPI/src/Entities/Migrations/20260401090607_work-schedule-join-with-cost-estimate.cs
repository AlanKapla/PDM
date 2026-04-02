using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class workschedulejoinwithcostestimate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateGroupId",
                table: "WorkScheduleStages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WorkScheduleStages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkScheduleStages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentStageId",
                table: "WorkScheduleStages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "WorkScheduleStages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateId",
                table: "WorkSchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_CostEstimateGroupId",
                table: "WorkScheduleStages",
                column: "CostEstimateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_ParentStageId",
                table: "WorkScheduleStages",
                column: "ParentStageId");

            // Bez uzupełnienia ProjectId indeks IX_WorkScheduleStages_TenantId_ProjectId nie będzie poprawnie działał
            // dla istniejących rekordów — domyślna wartość '00000000-...' nigdy nie pasowałaby do żadnego projektu.
            migrationBuilder.Sql(@"
                UPDATE wss
                SET wss.ProjectId = ws.ProjectId
                FROM WorkScheduleStages wss
                INNER JOIN WorkSchedules ws ON wss.WorkScheduleId = ws.Id
                WHERE wss.ProjectId = '00000000-0000-0000-0000-000000000000'
            ");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_TenantId_ProjectId",
                table: "WorkScheduleStages",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_WorkScheduleId_IsDeleted",
                table: "WorkScheduleStages",
                columns: new[] { "WorkScheduleId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_CostEstimateId",
                table: "WorkSchedules",
                column: "CostEstimateId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStages_WorkScheduleStages_ParentStageId",
                table: "WorkScheduleStages",
                column: "ParentStageId",
                principalTable: "WorkScheduleStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_CostEstimates_CostEstimateId",
                table: "WorkSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStages_CostEstimateGroups_CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStages_WorkScheduleStages_ParentStageId",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStages_CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStages_ParentStageId",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStages_TenantId_ProjectId",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStages_WorkScheduleId_IsDeleted",
                table: "WorkScheduleStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_CostEstimateId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CostEstimateGroupId",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "ParentStageId",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "WorkScheduleStages");

            migrationBuilder.DropColumn(
                name: "CostEstimateId",
                table: "WorkSchedules");
        }
    }
}
