using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class changechatentitites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_TenantMembers_TenantId_UserId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Projects_ProjectId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_TenantMembers_TenantId_CreatedByUserId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Tenants_TenantId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageHistories_TenantMembers_TenantId_UserId",
                table: "MessageHistories");

            migrationBuilder.DropIndex(
                name: "IX_MessageHistories_TenantId_UserId",
                table: "MessageHistories");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ProjectId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ProjectId_IsGroupChat",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_TenantId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_TenantId_CreatedByUserId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_ChatId_TenantId_UserId",
                table: "ChatMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_TenantId_UserId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MessageHistories");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ChatMembers");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MessageHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "MessageHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToMessageId",
                table: "MessageHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "ChatMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_ReplyToMessageId",
                table: "MessageHistories",
                column: "ReplyToMessageId",
                filter: "ReplyToMessageId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_ChatId_UserId",
                table: "ChatMembers",
                columns: new[] { "ChatId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageHistories_MessageHistories_ReplyToMessageId",
                table: "MessageHistories",
                column: "ReplyToMessageId",
                principalTable: "MessageHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageHistories_MessageHistories_ReplyToMessageId",
                table: "MessageHistories");

            migrationBuilder.DropIndex(
                name: "IX_MessageHistories_ReplyToMessageId",
                table: "MessageHistories");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_ChatId_UserId",
                table: "ChatMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MessageHistories");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "MessageHistories");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "MessageHistories");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "ChatMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MessageHistories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Chats",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Chats",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ChatMembers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_TenantId_UserId",
                table: "MessageHistories",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ProjectId",
                table: "Chats",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ProjectId_IsGroupChat",
                table: "Chats",
                columns: new[] { "ProjectId", "IsGroupChat" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_TenantId",
                table: "Chats",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_TenantId_CreatedByUserId",
                table: "Chats",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_ChatId_TenantId_UserId",
                table: "ChatMembers",
                columns: new[] { "ChatId", "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_TenantId_UserId",
                table: "ChatMembers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_TenantMembers_TenantId_UserId",
                table: "ChatMembers",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Projects_ProjectId",
                table: "Chats",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_TenantMembers_TenantId_CreatedByUserId",
                table: "Chats",
                columns: new[] { "TenantId", "CreatedByUserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Tenants_TenantId",
                table: "Chats",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageHistories_TenantMembers_TenantId_UserId",
                table: "MessageHistories",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "TenantMembers",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
