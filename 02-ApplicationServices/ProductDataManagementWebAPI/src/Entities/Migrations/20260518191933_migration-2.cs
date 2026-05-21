using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contractor",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "Place",
                table: "Costs");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractorId",
                table: "Costs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contractors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_ContractorId",
                table: "Costs",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_TenantId",
                table: "Contractors",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_TenantId_Name",
                table: "Contractors",
                columns: new[] { "TenantId", "Name" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_Contractors_ContractorId",
                table: "Costs",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costs_Contractors_ContractorId",
                table: "Costs");

            migrationBuilder.DropTable(
                name: "Contractors");

            migrationBuilder.DropIndex(
                name: "IX_Costs_ContractorId",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "Costs");

            migrationBuilder.AddColumn<string>(
                name: "Contractor",
                table: "Costs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Place",
                table: "Costs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
