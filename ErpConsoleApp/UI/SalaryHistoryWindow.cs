using System;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpConsoleApp.UI
{
    public class SalaryHistoryWindow : Window
    {
        public SalaryHistoryWindow(Employee employee) : base($"Salary History: {employee.Name} (Press ESC to back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            KeyDown += (e) => { if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; } };

            var list = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };

            try
            {
                using (var db = new AppDbContext())
                {
                    var history = db.Salaries
                        .Where(s => s.EmployeeId == employee.Id)
                        .OrderByDescending(s => s.PaymentDate)
                        .ToList();

                    var display = history.Select(s =>
                        $"{s.PaymentDate:MMM yyyy} | Paid: {s.FinalSalary:F2} | Days: {s.PresentDays} | Borrow Paid: {s.BorrowRepayment:F2}"
                    ).ToList();

                    if (display.Count == 0) display.Add("No history found.");
                    list.SetSource(display);
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }

            Add(list);

            var btnClose = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnClose.Clicked += () => Application.RequestStop();
            Add(btnClose);
        }
    }
}