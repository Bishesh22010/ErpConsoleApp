using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class RemovePinSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "LoginPin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Key", "Value" },
                values: new object[] { "LoginPin", "1234" });
        }
    }
}
