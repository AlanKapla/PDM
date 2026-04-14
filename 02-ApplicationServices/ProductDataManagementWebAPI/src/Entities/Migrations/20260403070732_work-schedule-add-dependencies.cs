using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class workscheduleadddependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "WorkScheduleStageWorks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE wsw
                SET wsw.ProjectId = ws.ProjectId
                FROM WorkScheduleStageWorks wsw
                INNER JOIN WorkScheduleStages wss ON wsw.WorkScheduleStageId = wss.Id
                INNER JOIN WorkSchedules ws ON wss.WorkScheduleId = ws.Id
            ");

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependencyType = table.Column<int>(type: "int", nullable: false),
                    LagDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkScheduleStageWorks_PredecessorWorkId",
                        column: x => x.PredecessorWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkScheduleStageWorks_SuccessorWorkId",
                        column: x => x.SuccessorWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_TenantId_ProjectId",
                table: "WorkScheduleStageWorks",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_PredecessorWorkId",
                table: "WorkScheduleStageWorkDependencies",
                column: "PredecessorWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_SuccessorWorkId",
                table: "WorkScheduleStageWorkDependencies",
                column: "SuccessorWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_TenantId_ProjectId",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_TenantId_WorkScheduleId",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "TenantId", "WorkScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_WorkScheduleId_PredecessorWorkId_SuccessorWorkId_DependencyType",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "WorkScheduleId", "PredecessorWorkId", "SuccessorWorkId", "DependencyType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkDependencies");

            migrationBuilder.DropIndex(
                name: "IX_WorkScheduleStageWorks_TenantId_ProjectId",
                table: "WorkScheduleStageWorks");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "WorkScheduleStageWorks");
        }
    }
}
