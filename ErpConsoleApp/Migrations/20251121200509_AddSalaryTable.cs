using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    SalaryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CalculationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SalaryAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PresentDays = table.Column<decimal>(type: "TEXT", nullable: false),
                    AbsentDays = table.Column<decimal>(type: "TEXT", nullable: false),
                    DeductionPerDay = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    BorrowRepayment = table.Column<decimal>(type: "TEXT", nullable: false),
                    FinalSalary = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.SalaryId);
                    table.ForeignKey(
                        name: "FK_Salaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_EmployeeId",
                table: "Salaries",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Salaries");
        }
    }
}
