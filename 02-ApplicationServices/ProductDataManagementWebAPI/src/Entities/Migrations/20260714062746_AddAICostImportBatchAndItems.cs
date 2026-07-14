using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddAICostImportBatchAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFileHashSha256",
                table: "Costs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AICostImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostDocumentType = table.Column<int>(type: "int", nullable: false),
                    TrackedCostContextJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    ProcessedFiles = table.Column<int>(type: "int", nullable: false),
                    PendingCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    DuplicateCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AICostImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AICostImportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ParsedDataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AICostImportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AICostImportItems_AICostImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "AICostImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId_SourceFileHashSha256",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "SourceFileHashSha256" });

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportBatches_TenantId_ProjectId",
                table: "AICostImportBatches",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportBatches_TenantId_ProjectId_Status",
                table: "AICostImportBatches",
                columns: new[] { "TenantId", "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportItems_AnalyzedAt_Status",
                table: "AICostImportItems",
                columns: new[] { "AnalyzedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportItems_BatchId",
                table: "AICostImportItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportItems_TenantId_ProjectId_FileHashSha256",
                table: "AICostImportItems",
                columns: new[] { "TenantId", "ProjectId", "FileHashSha256" });

            migrationBuilder.CreateIndex(
                name: "IX_AICostImportItems_TenantId_ProjectId_Status",
                table: "AICostImportItems",
                columns: new[] { "TenantId", "ProjectId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AICostImportItems");

            migrationBuilder.DropTable(
                name: "AICostImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_Costs_TenantId_ProjectId_SourceFileHashSha256",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "SourceFileHashSha256",
                table: "Costs");
        }
    }
}
