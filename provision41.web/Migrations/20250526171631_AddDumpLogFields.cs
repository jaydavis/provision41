using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Provision41.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDumpLogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DumpLogs_Trucks_TruckId",
                table: "DumpLogs");

            migrationBuilder.DropColumn(
                name: "PhotoUrlsJson",
                table: "DumpLogs");

            migrationBuilder.AlterColumn<string>(
                name: "TruckId",
                table: "DumpLogs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
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

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DumpLogs_Trucks_TruckId",
                table: "DumpLogs",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DumpLogs_Trucks_TruckId",
                table: "DumpLogs");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "DumpLogs");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "DumpLogs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "DumpLogs");

            migrationBuilder.AlterColumn<string>(
                name: "TruckId",
                table: "DumpLogs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrlsJson",
                table: "DumpLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_DumpLogs_Trucks_TruckId",
                table: "DumpLogs",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
