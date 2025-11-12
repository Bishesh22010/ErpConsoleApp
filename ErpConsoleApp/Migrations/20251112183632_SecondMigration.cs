using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ErpConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class SecondMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Parties",
                keyColumn: "PartyId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Parties",
                keyColumn: "PartyId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Parties",
                keyColumn: "PartyId",
                keyValue: 3);

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "PurchaseSlips",
                newName: "SlipDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SlipDate",
                table: "PurchaseSlips",
                newName: "Date");

            migrationBuilder.InsertData(
                table: "Parties",
                columns: new[] { "PartyId", "Name" },
                values: new object[,]
                {
                    { 1, "XYZ Party" },
                    { 2, "Main Supplier Inc." },
                    { 3, "Local Hardware" }
                });
        }
    }
}
