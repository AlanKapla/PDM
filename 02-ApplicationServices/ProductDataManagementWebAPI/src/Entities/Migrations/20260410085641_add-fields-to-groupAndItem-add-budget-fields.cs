using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addfieldstogroupAndItemaddbudgetfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetGross",
                table: "CostTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetNet",
                table: "CostTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CostEstimateItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CostEstimateGroups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE g
                SET g.Name = ISNULL(fv.StringValue, '')
                FROM CostEstimateGroups g
                LEFT JOIN CostEstimateGroupFieldValues fv
                    ON fv.GroupId = g.Id
                    AND fv.FieldDefinitionId IN (
                        SELECT Id FROM CostEstimateTemplateFieldDefinitionBase WHERE FieldType = 'GroupName'
                    )
            ");

            migrationBuilder.Sql(@"
                UPDATE i
                SET i.Name = ISNULL(fv.StringValue, '')
                FROM CostEstimateItems i
                LEFT JOIN CostEstimateItemFieldValues fv
                    ON fv.ItemId = i.Id
                    AND fv.FieldDefinitionId IN (
                        SELECT Id FROM CostEstimateTemplateFieldDefinitionBase WHERE FieldType = 'ItemSystemName'
                    )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetGross",
                table: "CostTrackers");

            migrationBuilder.DropColumn(
                name: "BudgetNet",
                table: "CostTrackers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CostEstimateItems");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CostEstimateGroups");
        }
    }
}
