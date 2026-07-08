using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addprojectcostcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "ProjectParams",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCostCategory_Code",
                table: "ProjectParams",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCostCategory_Name",
                table: "ProjectParams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectUnit_Order",
                table: "ProjectParams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Costs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ProjectCostCategory_Code" },
                unique: true,
                filter: "[ProjectCostCategory_Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_CategoryId",
                table: "Costs",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs",
                column: "CategoryId",
                principalTable: "ProjectParams",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.Sql("""
                WITH DefaultCategories AS (
                    SELECT * FROM (VALUES
                        (1, 'mat', N'Materiały budowlane'),
                        (2, 'rob', N'Robocizna'),
                        (3, 'sprzet', N'Sprzęt i maszyny'),
                        (4, 'transport', N'Transport i logistyka'),
                        (5, 'uslugi', N'Usługi zewnętrzne'),
                        (6, 'admin', N'Administracja i biuro'),
                        (7, 'media', N'Energia i media'),
                        (8, 'podwyk', N'Podwykonawcy'),
                        (9, 'narz', N'Narzędzia i wyposażenie'),
                        (10, 'inne', N'Inne')
                    ) AS v([Order], Code, Name)
                ),
                ProjectsWithoutCategories AS (
                    SELECT p.Id AS ProjectId
                    FROM Projects p
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ProjectParams pp
                        WHERE pp.ProjectId = p.Id AND pp.ParamType = 'CostCategory'
                    )
                )
                INSERT INTO ProjectParams (Id, ProjectId, ParamType, ProjectCostCategory_Code, ProjectCostCategory_Name, [Order])
                SELECT NEWID(), pwc.ProjectId, 'CostCategory', dc.Code, dc.Name, dc.[Order]
                FROM ProjectsWithoutCategories pwc
                CROSS JOIN DefaultCategories dc;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costs_ProjectParams_CategoryId",
                table: "Costs");

            migrationBuilder.DropIndex(
                name: "IX_ProjectParams_ProjectId_ProjectCostCategory_Code",
                table: "ProjectParams");

            migrationBuilder.DropIndex(
                name: "IX_Costs_CategoryId",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectCostCategory_Code",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectCostCategory_Name",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectUnit_Order",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Costs");
        }
    }
}
