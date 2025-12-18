using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationtenth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create ProjectFilePackages table first
            migrationBuilder.CreateTable(
                name: "ProjectFilePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFilePackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_TenantMembers_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_TenantMembers_TenantId_OwnerId",
                        columns: x => new { x.TenantId, x.OwnerId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 2. Create indexes on ProjectFilePackages
            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_CreatedByUserId",
                table: "ProjectFilePackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_OwnerId_ProjectId",
                table: "ProjectFilePackages",
                columns: new[] { "OwnerId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ProjectId_IsDeleted",
                table: "ProjectFilePackages",
                columns: new[] { "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ProjectId_TenantId",
                table: "ProjectFilePackages",
                columns: new[] { "ProjectId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_CreatedByUserId",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_OwnerId",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // 3. Migrate existing data: Create packages for each unique combination of TenantId, ProjectId, OwnerId, PackageName
            migrationBuilder.Sql(@"
                INSERT INTO ProjectFilePackages (Id, TenantId, ProjectId, OwnerId, Name, CreatedAt, CreatedByUserId, IsDeleted, DeletedAt)
                SELECT 
                    NEWID() AS Id,
                    pf.TenantId,
                    pf.ProjectId,
                    pf.OwnerId,
                    pf.PackageName AS Name,
                    MIN(pf.CreatedAt) AS CreatedAt,
                    pf.OwnerId AS CreatedByUserId,
                    0 AS IsDeleted,
                    NULL AS DeletedAt
                FROM ProjectFiles pf
                WHERE pf.IsDeleted = 0
                GROUP BY pf.TenantId, pf.ProjectId, pf.OwnerId, pf.PackageName
            ");

            // 4. Add ProjectFilePackageId column (nullable first)
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectFilePackageId",
                table: "ProjectFiles",
                type: "uniqueidentifier",
                nullable: true);

            // 5. Update ProjectFilePackageId with correct values (ALL records, not just non-deleted)
            migrationBuilder.Sql(@"
                UPDATE pf
                SET pf.ProjectFilePackageId = pfp.Id
                FROM ProjectFiles pf
                INNER JOIN ProjectFilePackages pfp 
                    ON pf.TenantId = pfp.TenantId 
                    AND pf.ProjectId = pfp.ProjectId 
                    AND pf.OwnerId = pfp.OwnerId 
                    AND pf.PackageName = pfp.Name
            ");
            
            // Handle any remaining NULL values (orphaned records without matching package)
            // Create a default package for any orphaned files
            migrationBuilder.Sql(@"
                -- Create packages for deleted files or files without matching package
                INSERT INTO ProjectFilePackages (Id, TenantId, ProjectId, OwnerId, Name, CreatedAt, CreatedByUserId, IsDeleted, DeletedAt)
                SELECT DISTINCT
                    NEWID() AS Id,
                    pf.TenantId,
                    pf.ProjectId,
                    pf.OwnerId,
                    ISNULL(pf.PackageName, 'Unknown') AS Name,
                    GETUTCDATE() AS CreatedAt,
                    pf.OwnerId AS CreatedByUserId,
                    1 AS IsDeleted,
                    GETUTCDATE() AS DeletedAt
                FROM ProjectFiles pf
                WHERE pf.ProjectFilePackageId IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ProjectFilePackages pfp 
                      WHERE pfp.TenantId = pf.TenantId 
                        AND pfp.ProjectId = pf.ProjectId 
                        AND pfp.OwnerId = pf.OwnerId 
                        AND pfp.Name = pf.PackageName
                  )
                GROUP BY pf.TenantId, pf.ProjectId, pf.OwnerId, pf.PackageName
            ");
            
            // Update orphaned files with their packages
            migrationBuilder.Sql(@"
                UPDATE pf
                SET pf.ProjectFilePackageId = pfp.Id
                FROM ProjectFiles pf
                INNER JOIN ProjectFilePackages pfp 
                    ON pf.TenantId = pfp.TenantId 
                    AND pf.ProjectId = pfp.ProjectId 
                    AND pf.OwnerId = pfp.OwnerId 
                    AND pf.PackageName = pfp.Name
                WHERE pf.ProjectFilePackageId IS NULL
            ");

            // 6. Make ProjectFilePackageId NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectFilePackageId",
                table: "ProjectFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 7. Create index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectFilePackageId",
                table: "ProjectFiles",
                column: "ProjectFilePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                table: "ProjectFiles",
                column: "ProjectFilePackageId",
                principalTable: "ProjectFilePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 8. Drop old index and column
            migrationBuilder.DropIndex(
                name: "IX_ProjectFiles_ProjectId_PackageName",
                table: "ProjectFiles");

            migrationBuilder.DropColumn(
                name: "PackageName",
                table: "ProjectFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Add PackageName column back
            migrationBuilder.AddColumn<string>(
                name: "PackageName",
                table: "ProjectFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // 2. Restore PackageName values from ProjectFilePackages
            migrationBuilder.Sql(@"
                UPDATE pf
                SET pf.PackageName = pfp.Name
                FROM ProjectFiles pf
                INNER JOIN ProjectFilePackages pfp ON pf.ProjectFilePackageId = pfp.Id
            ");

            // 3. Make PackageName NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "PackageName",
                table: "ProjectFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // 4. Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                table: "ProjectFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFiles_ProjectFilePackageId",
                table: "ProjectFiles");

            // 5. Drop ProjectFilePackageId column
            migrationBuilder.DropColumn(
                name: "ProjectFilePackageId",
                table: "ProjectFiles");

            // 6. Drop ProjectFilePackages table
            migrationBuilder.DropTable(
                name: "ProjectFilePackages");

            // 7. Recreate old index
            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_PackageName",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "PackageName" });
        }
    }
}
