using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class ViewPartySlips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "PurchaseSlips",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QtyType",
                table: "PurchaseSlips",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "PurchaseSlips",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SlipNumber",
                table: "PurchaseSlips",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseSlips",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "PurchaseSlips");

            migrationBuilder.DropColumn(
                name: "QtyType",
                table: "PurchaseSlips");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PurchaseSlips");

            migrationBuilder.DropColumn(
                name: "SlipNumber",
                table: "PurchaseSlips");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PurchaseSlips");
        }
    }
}
