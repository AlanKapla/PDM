using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class workscheduleaddisDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WorkSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_ProjectId_IsDeleted",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "ProjectId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_TenantId_ProjectId_IsDeleted",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkSchedules");
        }
    }
}
