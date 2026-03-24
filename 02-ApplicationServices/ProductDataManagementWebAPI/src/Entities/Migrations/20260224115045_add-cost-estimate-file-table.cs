using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addcostestimatefiletable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostEstimateFieldFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateFieldFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateFieldFiles_CostEstimateItemFieldValues_FieldValueId",
                        column: x => x.FieldValueId,
                        principalTable: "CostEstimateItemFieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateFieldFiles_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateFieldFiles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldFiles_CostEstimateId",
                table: "CostEstimateFieldFiles",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldFiles_CostEstimateId_IsDeleted",
                table: "CostEstimateFieldFiles",
                columns: new[] { "CostEstimateId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldFiles_CreatedByUserId",
                table: "CostEstimateFieldFiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldFiles_FieldValueId",
                table: "CostEstimateFieldFiles",
                column: "FieldValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateFieldFiles_IsDeleted",
                table: "CostEstimateFieldFiles",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimateFieldFiles");
        }
    }
}
