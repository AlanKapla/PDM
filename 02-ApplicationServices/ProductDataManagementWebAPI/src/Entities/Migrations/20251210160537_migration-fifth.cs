using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationfifth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "WorkScheduleStageWorks");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "WorkScheduleStageWorks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkPeriod",
                columns: table => new
                {
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkPeriod", x => new { x.WorkScheduleStageWorkId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkPeriod_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkPeriod");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "WorkScheduleStageWorks");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "WorkScheduleStageWorks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "WorkScheduleStageWorks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
