using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationsixth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Place = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HasDocument = table.Column<bool>(type: "bit", nullable: false),
                    DocumentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DocumentBlobPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_TenantMembers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_Date",
                table: "ProjectCosts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_ProjectId",
                table: "ProjectCosts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_TenantId_ProjectId_IsDeleted",
                table: "ProjectCosts",
                columns: new[] { "TenantId", "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_TenantId_UserId_IsDeleted",
                table: "ProjectCosts",
                columns: new[] { "TenantId", "UserId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectCosts");
        }
    }
}
