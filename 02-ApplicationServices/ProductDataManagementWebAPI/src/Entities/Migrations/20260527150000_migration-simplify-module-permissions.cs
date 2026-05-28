using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migrationsimplifymodulepermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [ProjectMemberModulePermissions];");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "ProjectMemberModulePermissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "ProjectMemberModulePermissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
