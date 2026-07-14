using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class fixcostcategoryindexfilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ProjectCostCategory_Code" },
                unique: true,
                filter: "[ProjectCostCategory_Code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ProjectCostCategory_Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");
        }
    }
}
