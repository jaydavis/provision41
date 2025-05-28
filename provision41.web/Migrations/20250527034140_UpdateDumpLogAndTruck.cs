using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Provision41.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDumpLogAndTruck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "DumpLogs");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "DumpLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "DumpLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
