using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationeighth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectCostId1",
                table: "SharedProjectCosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "ProjectCosts",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "ProjectCosts");
        }
    }
}
