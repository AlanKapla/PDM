using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class unifyinvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "TenantInvitations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TenantInvitations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantInvitationModulePermissions",
                columns: table => new
                {
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitationModulePermissions", x => new { x.InvitationId, x.Module });
                    table.ForeignKey(
                        name: "FK_TenantInvitationModulePermissions_TenantInvitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "TenantInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE ti
                SET
                    ti.ProjectId = pi.ProjectId,
                    ti.IsAdmin = pi.IsAdmin,
                    ti.ExpiresAt = CASE WHEN pi.ExpiresAt > ti.ExpiresAt THEN pi.ExpiresAt ELSE ti.ExpiresAt END,
                    ti.InvitedByUserId = pi.InvitedByUserId,
                    ti.Status = pi.Status,
                    ti.IsActive = pi.IsActive,
                    ti.AcceptedAt = COALESCE(pi.AcceptedAt, ti.AcceptedAt)
                FROM TenantInvitations ti
                INNER JOIN ProjectInvitations pi ON pi.TenantInvitationId = ti.Id
                WHERE pi.TenantInvitationId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO TenantInvitations (
                    Id, TenantId, ProjectId, Email, Token, CreatedAt, InvitedByUserId,
                    ExpiresAt, AcceptedAt, IsActive, Status, IsAdmin)
                SELECT
                    pi.Id,
                    pi.TenantId,
                    pi.ProjectId,
                    pi.Email,
                    pi.Token,
                    pi.CreatedAt,
                    pi.InvitedByUserId,
                    pi.ExpiresAt,
                    pi.AcceptedAt,
                    pi.IsActive,
                    pi.Status,
                    pi.IsAdmin
                FROM ProjectInvitations pi
                WHERE pi.TenantInvitationId IS NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM TenantInvitations ti
                        WHERE ti.TenantId = pi.TenantId
                            AND ti.Email = pi.Email
                            AND ti.IsActive = 1
                            AND ti.Status = 'Pending');
                """);

            migrationBuilder.Sql("""
                UPDATE ti
                SET
                    ti.ProjectId = pi.ProjectId,
                    ti.IsAdmin = pi.IsAdmin,
                    ti.ExpiresAt = CASE WHEN pi.ExpiresAt > ti.ExpiresAt THEN pi.ExpiresAt ELSE ti.ExpiresAt END,
                    ti.InvitedByUserId = pi.InvitedByUserId
                FROM TenantInvitations ti
                INNER JOIN ProjectInvitations pi ON pi.TenantInvitationId IS NULL
                    AND pi.TenantId = ti.TenantId
                    AND pi.Email = ti.Email
                    AND ti.IsActive = 1
                    AND ti.Status = 'Pending'
                    AND ti.ProjectId IS NULL
                WHERE NOT EXISTS (
                    SELECT 1 FROM ProjectInvitations linked
                    WHERE linked.TenantInvitationId = ti.Id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO TenantInvitationModulePermissions (InvitationId, Module)
                SELECT
                    COALESCE(pi.TenantInvitationId, pi.Id),
                    pim.Module
                FROM ProjectInvitationModulePermissions pim
                INNER JOIN ProjectInvitations pi ON pi.Id = pim.InvitationId
                WHERE COALESCE(pi.TenantInvitationId, pi.Id) IN (SELECT Id FROM TenantInvitations);
                """);

            migrationBuilder.DropTable(
                name: "ProjectInvitationModulePermissions");

            migrationBuilder.DropTable(
                name: "ProjectInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_ProjectId",
                table: "TenantInvitations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email_ProjectId",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email", "ProjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantInvitations_Projects_ProjectId",
                table: "TenantInvitations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantInvitations_Projects_ProjectId",
                table: "TenantInvitations");

            migrationBuilder.DropTable(
                name: "TenantInvitationModulePermissions");

            migrationBuilder.DropIndex(
                name: "IX_TenantInvitations_ProjectId",
                table: "TenantInvitations");

            migrationBuilder.DropIndex(
                name: "IX_TenantInvitations_TenantId_Email_ProjectId",
                table: "TenantInvitations");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "TenantInvitations");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TenantInvitations");

            migrationBuilder.CreateTable(
                name: "ProjectInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantInvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_TenantInvitations_TenantInvitationId",
                        column: x => x.TenantInvitationId,
                        principalTable: "TenantInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectInvitationModulePermissions",
                columns: table => new
                {
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvitationModulePermissions", x => new { x.InvitationId, x.Module });
                    table.ForeignKey(
                        name: "FK_ProjectInvitationModulePermissions_ProjectInvitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "ProjectInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_ExpiresAt",
                table: "ProjectInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_InvitedByUserId",
                table: "ProjectInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_ProjectId",
                table: "ProjectInvitations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TenantId_ProjectId_Email",
                table: "ProjectInvitations",
                columns: new[] { "TenantId", "ProjectId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TenantId_ProjectId_Status",
                table: "ProjectInvitations",
                columns: new[] { "TenantId", "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TenantInvitationId",
                table: "ProjectInvitations",
                column: "TenantInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_Token",
                table: "ProjectInvitations",
                column: "Token",
                unique: true);
        }
    }
}
