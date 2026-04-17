using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class workschedulesupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkScheduleStageWorkPeriod",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "WorkScheduleStageWorks");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndDate",
                table: "WorkScheduleStageWorks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartDate",
                table: "WorkScheduleStageWorks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkScheduleStageWorks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "WorkScheduleStageWorkPeriod",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "WorkScheduleStageWorkPeriod",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "WorkScheduleStageWorkPeriod",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkScheduleStageWorkPeriod",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkScheduleStages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossAmount",
                table: "ProjectCosts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkScheduleStageWorkPeriod",
                table: "WorkScheduleStageWorkPeriod",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkPeriod_TenantId_ProjectId",
                table: "WorkScheduleStageWorkPeriod",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkPeriod_WorkScheduleStageWorkId_StartDate",
                table: "WorkScheduleStageWorkPeriod",
                columns: new[] { "WorkScheduleStageWorkId", "StartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkScheduleStageWorkPeriod",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkPeriod_TenantId_ProjectId",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorkPeriod_WorkScheduleStageWorkId_StartDate",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropColumn(
                name: "PlannedEndDate",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "PlannedStartDate",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkScheduleStages");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "WorkScheduleStageWorks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "WorkScheduleStageWorkPeriod",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkScheduleStageWorkPeriod");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WorkScheduleStageWorkPeriod",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossAmount",
                table: "ProjectCosts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkScheduleStageWorkPeriod",
                table: "WorkScheduleStageWorkPeriod",
                columns: new[] { "WorkScheduleStageWorkId", "Id" });
        }
    }
}
