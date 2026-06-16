using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectParams_ProjectId_ParamType",
                table: "ProjectParams");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "ProjectParams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectUnit_Code",
                table: "ProjectParams",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectUnit_Name",
                table: "ProjectParams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectUnit_Symbol",
                table: "ProjectParams",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ParamType",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ParamType" },
                unique: true,
                filter: "[ParamType] = 'Currency'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectParams_ProjectId_ParamType",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectUnit_Code",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectUnit_Name",
                table: "ProjectParams");

            migrationBuilder.DropColumn(
                name: "ProjectUnit_Symbol",
                table: "ProjectParams");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ParamType",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ParamType" },
                unique: true);
        }
    }
}
