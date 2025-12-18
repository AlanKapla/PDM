using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationninth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedProjectCosts_ProjectCosts_ProjectCostId1",
                table: "SharedProjectCosts");

            migrationBuilder.DropIndex(
                name: "IX_SharedProjectCosts_ProjectCostId1",
                table: "SharedProjectCosts");

            migrationBuilder.DropColumn(
                name: "ProjectCostId1",
                table: "SharedProjectCosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectCostId1",
                table: "SharedProjectCosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectCosts_ProjectCostId1",
                table: "SharedProjectCosts",
                column: "ProjectCostId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedProjectCosts_ProjectCosts_ProjectCostId1",
                table: "SharedProjectCosts",
                column: "ProjectCostId1",
                principalTable: "ProjectCosts",
                principalColumn: "Id");
        }
    }
}
