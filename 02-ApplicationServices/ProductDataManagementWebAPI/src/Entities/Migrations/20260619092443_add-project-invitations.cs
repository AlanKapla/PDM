using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class addprojectinvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false)
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
                name: "IX_ProjectInvitations_Token",
                table: "ProjectInvitations",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInvitationModulePermissions");

            migrationBuilder.DropTable(
                name: "ProjectInvitations");
        }
    }
}
