using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationeleventh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedProjectFiles_ProjectFiles_ProjectFileId1",
                table: "SharedProjectFiles");

            migrationBuilder.DropIndex(
                name: "IX_SharedProjectFiles_ProjectFileId1",
                table: "SharedProjectFiles");

            migrationBuilder.DropColumn(
                name: "ProjectFileId1",
                table: "SharedProjectFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectFileId1",
                table: "SharedProjectFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFileId1",
                table: "SharedProjectFiles",
                column: "ProjectFileId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedProjectFiles_ProjectFiles_ProjectFileId1",
                table: "SharedProjectFiles",
                column: "ProjectFileId1",
                principalTable: "ProjectFiles",
                principalColumn: "Id");
        }
    }
}
