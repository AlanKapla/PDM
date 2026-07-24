using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addworkscheduleassignmentidandcontractor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkScheduleStageWorkAssignments",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WorkScheduleStageWorkAssignments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "WorkScheduleStageWorkAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddColumn<Guid>(
                name: "ContractorId",
                table: "WorkScheduleStageWorkAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkScheduleStageWorkAssignments",
                table: "WorkScheduleStageWorkAssignments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_ContractorId",
                table: "WorkScheduleStageWorkAssignments",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_WorkScheduleStageWorkId_ContractorId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "WorkScheduleStageWorkId", "ContractorId" },
                unique: true,
                filter: "[ContractorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_WorkScheduleStageWorkId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "WorkScheduleStageWorkId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkScheduleStageWorkAssignments_AssigneeXor",
                table: "WorkScheduleStageWorkAssignments",
                sql: "([UserId] IS NOT NULL AND [ContractorId] IS NULL) OR ([UserId] IS NULL AND [ContractorId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Contractors_ContractorId",
                table: "WorkScheduleStageWorkAssignments",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkScheduleStageWorkAssignments_Contractors_ContractorId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkScheduleStageWorkAssignments",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkAssignments_ContractorId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkAssignments_WorkScheduleStageWorkId_ContractorId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkAssignments_WorkScheduleStageWorkId_UserId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkScheduleStageWorkAssignments_AssigneeXor",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "WorkScheduleStageWorkAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WorkScheduleStageWorkAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkScheduleStageWorkAssignments",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "WorkScheduleStageWorkId", "TenantId", "ProjectId", "UserId" });
        }
    }
}
