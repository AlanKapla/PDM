using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addtechnicaldocumentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTechnicalDocumentations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoRetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTechnicalDocumentations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTechnicalDocumentations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTechnicalDocumentations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTechnicalDocumentationFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalDocumentationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTechnicalDocumentationFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTechnicalDocumentationFiles_ProjectTechnicalDocumentations_TechnicalDocumentationId",
                        column: x => x.TechnicalDocumentationId,
                        principalTable: "ProjectTechnicalDocumentations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalDocumentationFiles_TechnicalDocumentationId",
                table: "ProjectTechnicalDocumentationFiles",
                column: "TechnicalDocumentationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalDocumentationFiles_TenantId_ProjectId_TechnicalDocumentationId",
                table: "ProjectTechnicalDocumentationFiles",
                columns: new[] { "TenantId", "ProjectId", "TechnicalDocumentationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalDocumentations_CreatedByUserId",
                table: "ProjectTechnicalDocumentations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalDocumentations_ProjectId",
                table: "ProjectTechnicalDocumentations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalDocumentations_TenantId_ProjectId",
                table: "ProjectTechnicalDocumentations",
                columns: new[] { "TenantId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTechnicalDocumentationFiles");

            migrationBuilder.DropTable(
                name: "ProjectTechnicalDocumentations");
        }
    }
}
