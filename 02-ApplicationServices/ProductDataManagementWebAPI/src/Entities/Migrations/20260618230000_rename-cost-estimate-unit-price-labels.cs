using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class renamecostestimateunitpricelabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE CostEstimateFieldSchemas
                SET FieldName = N'Cena netto'
                WHERE FieldKey = N'unitPriceNet'
                  AND FieldName = N'Cena jednostkowa netto'
                """);

            migrationBuilder.Sql(
                """
                UPDATE CostEstimateFieldSchemas
                SET FieldName = N'Cena brutto'
                WHERE FieldKey = N'unitPriceGross'
                  AND FieldName = N'Cena jednostkowa brutto'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE CostEstimateFieldSchemas
                SET FieldName = N'Cena jednostkowa netto'
                WHERE FieldKey = N'unitPriceNet'
                  AND FieldName = N'Cena netto'
                """);

            migrationBuilder.Sql(
                """
                UPDATE CostEstimateFieldSchemas
                SET FieldName = N'Cena jednostkowa brutto'
                WHERE FieldKey = N'unitPriceGross'
                  AND FieldName = N'Cena brutto'
                """);
        }
    }
}
