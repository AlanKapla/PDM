using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addprojectcosttrackedcostlink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_ProjectCostTrackedCostLinks_ProjectCostId",
                table: "ProjectCostTrackedCostLinks",
                column: "ProjectCostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCostTrackedCostLinks_TrackedCostId",
                table: "ProjectCostTrackedCostLinks",
                column: "TrackedCostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectCostTrackedCostLinks");
        }
    }
}
