using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class fixcategoryfknoaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs");

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs",
                column: "CategoryId",
                principalTable: "ProjectParams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs");

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs",
                column: "CategoryId",
                principalTable: "ProjectParams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
