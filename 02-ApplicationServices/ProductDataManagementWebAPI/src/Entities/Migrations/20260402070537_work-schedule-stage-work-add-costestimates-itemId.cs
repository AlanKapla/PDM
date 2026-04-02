using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class workschedulestageworkaddcostestimatesitemId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CostEstimateItemId",
                table: "WorkScheduleStageWorks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_CostEstimateItemId",
                table: "WorkScheduleStageWorks",
                column: "CostEstimateItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStageWorks_CostEstimateItems_CostEstimateItemId",
                table: "WorkScheduleStageWorks",
                column: "CostEstimateItemId",
                principalTable: "CostEstimateItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStageWorks_CostEstimateItems_CostEstimateItemId",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorks_CostEstimateItemId",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "CostEstimateItemId",
                table: "WorkScheduleStageWorks");
        }
    }
}
