using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addcategoriesforcostestimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostEstimateTemplateCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateTemplateCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateTemplateCategories_CostEstimateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CostEstimateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateCategories_TemplateId",
                table: "CostEstimateTemplateCategories",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateCategories_TemplateId_Name",
                table: "CostEstimateTemplateCategories",
                columns: new[] { "TemplateId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimateTemplateCategories");
        }
    }
}
