using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationfourth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_TenantMembers_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStages_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ColorRgb = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorks_WorkScheduleStages_WorkScheduleStageId",
                        column: x => x.WorkScheduleStageId,
                        principalTable: "WorkScheduleStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkAssignments",
                columns: table => new
                {
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkAssignments", x => new { x.WorkScheduleStageWorkId, x.TenantId, x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_ProjectMembers_TenantId_ProjectId_UserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.UserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ProjectId",
                table: "WorkSchedules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_CreatedByUserId",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_ProjectId",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_WorkScheduleId_Order",
                table: "WorkScheduleStages",
                columns: new[] { "WorkScheduleId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_TenantId_ProjectId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "TenantId", "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_WorkScheduleStageId_Order",
                table: "WorkScheduleStageWorks",
                columns: new[] { "WorkScheduleStageId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorks");

            migrationBuilder.DropTable(
                name: "WorkScheduleStages");

            migrationBuilder.DropTable(
                name: "WorkSchedules");
        }
    }
}
