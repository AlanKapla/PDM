using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class linkprojectinvitationstotenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantInvitationId",
                table: "ProjectInvitations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TenantInvitationId",
                table: "ProjectInvitations",
                column: "TenantInvitationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectInvitations_TenantInvitations_TenantInvitationId",
                table: "ProjectInvitations",
                column: "TenantInvitationId",
                principalTable: "TenantInvitations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectInvitations_TenantInvitations_TenantInvitationId",
                table: "ProjectInvitations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInvitations_TenantInvitationId",
                table: "ProjectInvitations");

            migrationBuilder.DropColumn(
                name: "TenantInvitationId",
                table: "ProjectInvitations");
        }
    }
}
