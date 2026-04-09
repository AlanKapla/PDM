using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addcosttracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create CostTrackers table first — no FK dependencies at this point
            migrationBuilder.CreateTable(
                name: "CostTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostTrackers", x => x.Id);
                });

            // 2. Add CostTrackerId as nullable so existing rows don't get a duplicate default
            migrationBuilder.AddColumn<Guid>(
                name: "CostTrackerId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostTrackerId",
                table: "CostEstimates",
                type: "uniqueidentifier",
                nullable: true);

            // 3. Backfill: create one CostTracker per existing project, then link it
            migrationBuilder.Sql(@"
                INSERT INTO CostTrackers (Id, TenantId, ProjectId)
                SELECT NEWID(), TenantId, Id FROM Projects;

                UPDATE p
                SET p.CostTrackerId = ct.Id
                FROM Projects p
                INNER JOIN CostTrackers ct ON ct.ProjectId = p.Id;
            ");

            // 4. Now that every project has a unique tracker, make the column non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "CostTrackerId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TrackedCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TrackerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostEstimateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Net = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Gross = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Contractor = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedCosts_CostEstimateItems_CostEstimateItemId",
                        column: x => x.CostEstimateItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackedCosts_CostTrackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "CostTrackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrackedCostAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TrackedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCostAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedCostAttachments_TrackedCosts_TrackedCostId",
                        column: x => x.TrackedCostId,
                        principalTable: "TrackedCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CostTrackerId",
                table: "Projects",
                column: "CostTrackerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_CostTrackerId",
                table: "CostEstimates",
                column: "CostTrackerId",
                unique: true,
                filter: "[CostTrackerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackers_ProjectId",
                table: "CostTrackers",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackers_TenantId",
                table: "CostTrackers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCostAttachments_IsDeleted",
                table: "TrackedCostAttachments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCostAttachments_TrackedCostId",
                table: "TrackedCostAttachments",
                column: "TrackedCostId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_CostEstimateItemId",
                table: "TrackedCosts",
                column: "CostEstimateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_IsDeleted",
                table: "TrackedCosts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCosts_TrackerId",
                table: "TrackedCosts",
                column: "TrackerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimates_CostTrackers_CostTrackerId",
                table: "CostEstimates",
                column: "CostTrackerId",
                principalTable: "CostTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_CostTrackers_CostTrackerId",
                table: "Projects",
                column: "CostTrackerId",
                principalTable: "CostTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimates_CostTrackers_CostTrackerId",
                table: "CostEstimates");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_CostTrackers_CostTrackerId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "TrackedCostAttachments");

            migrationBuilder.DropTable(
                name: "TrackedCosts");

            migrationBuilder.DropTable(
                name: "CostTrackers");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CostTrackerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimates_CostTrackerId",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "CostTrackerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CostTrackerId",
                table: "CostEstimates");
        }
    }
}
