using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addcostestimatefieldschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimateAdditionalFieldValues_CostEstimateAdditionalFields_AdditionalFieldId",
                table: "CostEstimateAdditionalFieldValues");

            migrationBuilder.RenameTable(
                name: "CostEstimateAdditionalFields",
                newName: "CostEstimateFieldSchemas");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CostEstimateFieldSchemas",
                newName: "FieldName");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateAdditionalFields_CostEstimateId",
                table: "CostEstimateFieldSchemas",
                newName: "IX_CostEstimateFieldSchemas_CostEstimateId");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateAdditionalFields_CostEstimateId_Order",
                table: "CostEstimateFieldSchemas",
                newName: "IX_CostEstimateFieldSchemas_CostEstimateId_Order");

            migrationBuilder.AddColumn<string>(
                name: "FieldKey",
                table: "CostEstimateFieldSchemas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdditionalField",
                table: "CostEstimateFieldSchemas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBasicField",
                table: "CostEstimateFieldSchemas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE CostEstimateFieldSchemas
                SET FieldKey = CAST(Id AS nvarchar(64)),
                    IsAdditionalField = 1,
                    IsBasicField = 0
                """);

            migrationBuilder.RenameColumn(
                name: "AdditionalFieldId",
                table: "CostEstimateAdditionalFieldValues",
                newName: "FieldSchemaId");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateAdditionalFieldValues_AdditionalFieldId",
                table: "CostEstimateAdditionalFieldValues",
                newName: "IX_CostEstimateAdditionalFieldValues_FieldSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldSchemas_CostEstimateId_FieldKey",
                table: "CostEstimateFieldSchemas",
                columns: new[] { "CostEstimateId", "FieldKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimateAdditionalFieldValues_CostEstimateFieldSchemas_FieldSchemaId",
                table: "CostEstimateAdditionalFieldValues",
                column: "FieldSchemaId",
                principalTable: "CostEstimateFieldSchemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            SeedDefaultBasicFields(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM CostEstimateFieldSchemas WHERE IsBasicField = 1
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimateAdditionalFieldValues_CostEstimateFieldSchemas_FieldSchemaId",
                table: "CostEstimateAdditionalFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimateFieldSchemas_CostEstimateId_FieldKey",
                table: "CostEstimateFieldSchemas");

            migrationBuilder.DropColumn(
                name: "FieldKey",
                table: "CostEstimateFieldSchemas");

            migrationBuilder.DropColumn(
                name: "IsAdditionalField",
                table: "CostEstimateFieldSchemas");

            migrationBuilder.DropColumn(
                name: "IsBasicField",
                table: "CostEstimateFieldSchemas");

            migrationBuilder.RenameColumn(
                name: "FieldSchemaId",
                table: "CostEstimateAdditionalFieldValues",
                newName: "AdditionalFieldId");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateAdditionalFieldValues_FieldSchemaId",
                table: "CostEstimateAdditionalFieldValues",
                newName: "IX_CostEstimateAdditionalFieldValues_AdditionalFieldId");

            migrationBuilder.RenameColumn(
                name: "FieldName",
                table: "CostEstimateFieldSchemas",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateFieldSchemas_CostEstimateId",
                table: "CostEstimateFieldSchemas",
                newName: "IX_CostEstimateAdditionalFields_CostEstimateId");

            migrationBuilder.RenameIndex(
                name: "IX_CostEstimateFieldSchemas_CostEstimateId_Order",
                table: "CostEstimateFieldSchemas",
                newName: "IX_CostEstimateAdditionalFields_CostEstimateId_Order");

            migrationBuilder.RenameTable(
                name: "CostEstimateFieldSchemas",
                newName: "CostEstimateAdditionalFields");

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimateAdditionalFieldValues_CostEstimateAdditionalFields_AdditionalFieldId",
                table: "CostEstimateAdditionalFieldValues",
                column: "AdditionalFieldId",
                principalTable: "CostEstimateAdditionalFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void SeedDefaultBasicFields(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO CostEstimateFieldSchemas (Id, CostEstimateId, FieldName, FieldKey, FieldType, IsBasicField, IsAdditionalField, [Order], CreatedAt)
                SELECT NEWID(), ce.Id, defs.FieldName, defs.FieldKey, defs.FieldType, 1, 0, defs.[Order], GETUTCDATE()
                FROM CostEstimates ce
                CROSS APPLY (VALUES
                    (N'Nazwa', N'name', 100, 0),
                    (N'Akcje', N'actions', 112, 1),
                    (N'Ilość', N'quantity', 101, 2),
                    (N'Jednostka', N'unit', 102, 3),
                    (N'Cena jednostkowa netto', N'unitPriceNet', 103, 4),
                    (N'Stawka VAT', N'vatRate', 104, 5),
                    (N'Cena jednostkowa brutto', N'unitPriceGross', 105, 6),
                    (N'Wartość netto', N'netValue', 106, 7),
                    (N'Wartość brutto', N'grossValue', 107, 8),
                    (N'Wartość VAT', N'vatValue', 108, 9),
                    (N'Sumuj', N'isSelected', 109, 10),
                    (N'Zakres harmonogramu', N'isStageWork', 110, 11),
                    (N'Plik', N'files', 111, 12)
                ) AS defs(FieldName, FieldKey, FieldType, [Order])
                WHERE ce.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM CostEstimateFieldSchemas fs
                      WHERE fs.CostEstimateId = ce.Id AND fs.IsBasicField = 1
                  )
                """);
        }
    }
}
