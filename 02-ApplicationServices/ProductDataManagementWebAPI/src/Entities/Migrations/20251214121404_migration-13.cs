using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimates_CostEstimateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CostEstimateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_CreatedAt",
                table: "CostEstimates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_IsDeleted",
                table: "CostEstimates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_OwnerId",
                table: "CostEstimates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_ProjectId",
                table: "CostEstimates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_Status",
                table: "CostEstimates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_TemplateId",
                table: "CostEstimates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_TenantId",
                table: "CostEstimates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_TenantId_ProjectId",
                table: "CostEstimates",
                columns: new[] { "TenantId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimates");
        }
    }
}
