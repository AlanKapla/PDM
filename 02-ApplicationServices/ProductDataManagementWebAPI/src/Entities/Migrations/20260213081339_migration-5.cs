using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedProjectFiles_ProjectFileId_SharedWithUserId",
                table: "SharedProjectFiles");

            migrationBuilder.DropColumn(
                name: "TemplateStructure",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "CostEstimates");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectFileId",
                table: "SharedProjectFiles",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Access",
                table: "SharedProjectFiles",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectFilePackageId",
                table: "SharedProjectFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "AutoNumberGroups",
                table: "CostEstimateTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAddGroups",
                table: "CostEstimateTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanBranchGroups",
                table: "CostEstimateTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CostEstimateTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupNumberFormat",
                table: "CostEstimateTemplates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxGroupLevel",
                table: "CostEstimateTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedCurrencyId",
                table: "CostEstimates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVat",
                table: "CostEstimates",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostEstimateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroups_CostEstimateGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroups_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateTemplateCurrencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateTemplateCurrencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateTemplateCurrencies_CostEstimateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CostEstimateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateTemplateFieldDefinitionBase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldScope = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSortable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ParentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FieldDefinitionType = table.Column<string>(type: "nvarchar(55)", maxLength: 55, nullable: false),
                    SumInGroup = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    SumInTotal = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateTemplateFieldDefinitionBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateTemplateFieldDefinitionBase_CostEstimateTemplateFieldDefinitionBase_ParentFieldId",
                        column: x => x.ParentFieldId,
                        principalTable: "CostEstimateTemplateFieldDefinitionBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateTemplateFieldDefinitionBase_CostEstimateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CostEstimateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateTemplateUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateTemplateUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateTemplateUnits_CostEstimateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CostEstimateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelationType = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "None"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    NetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimateGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimateItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateGroupFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateGroupFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroupFieldValues_CostEstimateGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroupFieldValues_CostEstimateTemplateFieldDefinitionBase_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CostEstimateTemplateFieldDefinitionBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItemCostEstimateItem",
                columns: table => new
                {
                    ComponentsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItemCostEstimateItem", x => new { x.ComponentsId, x.OptionsId });
                    table.ForeignKey(
                        name: "FK_CostEstimateItemCostEstimateItem_CostEstimateItems_ComponentsId",
                        column: x => x.ComponentsId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemCostEstimateItem_CostEstimateItems_OptionsId",
                        column: x => x.OptionsId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItemFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItemFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemFieldValues_CostEstimateItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemFieldValues_CostEstimateTemplateFieldDefinitionBase_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CostEstimateTemplateFieldDefinitionBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFileId",
                table: "SharedProjectFiles",
                column: "ProjectFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_ProjectFileId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "ProjectFilePackageId", "ProjectFileId", "SharedWithUserId" },
                unique: true,
                filter: "[ProjectFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "ProjectFilePackageId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplates_Category",
                table: "CostEstimateTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_SelectedCurrencyId",
                table: "CostEstimates",
                column: "SelectedCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupFieldValues_FieldDefinitionId",
                table: "CostEstimateGroupFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupFieldValues_GroupId",
                table: "CostEstimateGroupFieldValues",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroupFieldValues_GroupId_FieldDefinitionId",
                table: "CostEstimateGroupFieldValues",
                columns: new[] { "GroupId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId",
                table: "CostEstimateGroups",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId_Level",
                table: "CostEstimateGroups",
                columns: new[] { "CostEstimateId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId_ParentGroupId",
                table: "CostEstimateGroups",
                columns: new[] { "CostEstimateId", "ParentGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_IsDeleted",
                table: "CostEstimateGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_ParentGroupId",
                table: "CostEstimateGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_ParentGroupId_Order",
                table: "CostEstimateGroups",
                columns: new[] { "ParentGroupId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemCostEstimateItem_OptionsId",
                table: "CostEstimateItemCostEstimateItem",
                column: "OptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFieldValues_FieldDefinitionId",
                table: "CostEstimateItemFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFieldValues_ItemId",
                table: "CostEstimateItemFieldValues",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFieldValues_ItemId_FieldDefinitionId",
                table: "CostEstimateItemFieldValues",
                columns: new[] { "ItemId", "FieldDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_CostEstimateId",
                table: "CostEstimateItems",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_GroupId",
                table: "CostEstimateItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_GroupId_Order",
                table: "CostEstimateItems",
                columns: new[] { "GroupId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_IsDeleted",
                table: "CostEstimateItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_ParentItemId",
                table: "CostEstimateItems",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_ParentItemId_RelationType",
                table: "CostEstimateItems",
                columns: new[] { "ParentItemId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateCurrencies_TemplateId",
                table: "CostEstimateTemplateCurrencies",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateCurrencies_TemplateId_Code",
                table: "CostEstimateTemplateCurrencies",
                columns: new[] { "TemplateId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateCurrencies_TemplateId_IsDefault",
                table: "CostEstimateTemplateCurrencies",
                columns: new[] { "TemplateId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedFieldDefinition_TemplateId_FieldType",
                table: "CostEstimateTemplateFieldDefinitionBase",
                columns: new[] { "TemplateId", "FieldType" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateFieldDefinitionBase_ParentFieldId",
                table: "CostEstimateTemplateFieldDefinitionBase",
                column: "ParentFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitionBase_Order",
                table: "CostEstimateTemplateFieldDefinitionBase",
                columns: new[] { "TemplateId", "FieldScope", "ParentFieldId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitionBase_TemplateId_FieldName",
                table: "CostEstimateTemplateFieldDefinitionBase",
                columns: new[] { "TemplateId", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_GenericFieldDefinition_TemplateId_FieldType",
                table: "CostEstimateTemplateFieldDefinitionBase",
                columns: new[] { "TemplateId", "FieldType" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateUnits_TemplateId",
                table: "CostEstimateTemplateUnits",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateUnits_TemplateId_Category",
                table: "CostEstimateTemplateUnits",
                columns: new[] { "TemplateId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateUnits_TemplateId_Code",
                table: "CostEstimateTemplateUnits",
                columns: new[] { "TemplateId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateTemplateUnits_TemplateId_IsDefault",
                table: "CostEstimateTemplateUnits",
                columns: new[] { "TemplateId", "IsDefault" });

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimates_CostEstimateTemplateCurrencies_SelectedCurrencyId",
                table: "CostEstimates",
                column: "SelectedCurrencyId",
                principalTable: "CostEstimateTemplateCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                table: "SharedProjectFiles",
                column: "ProjectFilePackageId",
                principalTable: "ProjectFilePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimates_CostEstimateTemplateCurrencies_SelectedCurrencyId",
                table: "CostEstimates");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                table: "SharedProjectFiles");

            migrationBuilder.DropTable(
                name: "CostEstimateGroupFieldValues");

            migrationBuilder.DropTable(
                name: "CostEstimateItemCostEstimateItem");

            migrationBuilder.DropTable(
                name: "CostEstimateItemFieldValues");

            migrationBuilder.DropTable(
                name: "CostEstimateTemplateCurrencies");

            migrationBuilder.DropTable(
                name: "CostEstimateTemplateUnits");

            migrationBuilder.DropTable(
                name: "CostEstimateItems");

            migrationBuilder.DropTable(
                name: "CostEstimateTemplateFieldDefinitionBase");

            migrationBuilder.DropTable(
                name: "CostEstimateGroups");

            migrationBuilder.DropIndex(
                name: "IX_SharedProjectFiles_ProjectFileId",
                table: "SharedProjectFiles");

            migrationBuilder.DropIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_ProjectFileId_SharedWithUserId",
                table: "SharedProjectFiles");

            migrationBuilder.DropIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_SharedWithUserId",
                table: "SharedProjectFiles");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimateTemplates_Category",
                table: "CostEstimateTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimates_SelectedCurrencyId",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "SharedProjectFiles");

            migrationBuilder.DropColumn(
                name: "ProjectFilePackageId",
                table: "SharedProjectFiles");

            migrationBuilder.DropColumn(
                name: "AutoNumberGroups",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "CanAddGroups",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "CanBranchGroups",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "GroupNumberFormat",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "MaxGroupLevel",
                table: "CostEstimateTemplates");

            migrationBuilder.DropColumn(
                name: "SelectedCurrencyId",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "TotalVat",
                table: "CostEstimates");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectFileId",
                table: "SharedProjectFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateStructure",
                table: "CostEstimateTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "CostEstimates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFileId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "ProjectFileId", "SharedWithUserId" },
                unique: true);
        }
    }
}
