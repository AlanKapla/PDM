using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkComments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkComments_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkComments_CreatedByUserId",
                table: "WorkScheduleStageWorkComments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkComments_WorkScheduleStageWorkId_CreatedAt",
                table: "WorkScheduleStageWorkComments",
                columns: new[] { "WorkScheduleStageWorkId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkComments");
        }
    }
}
