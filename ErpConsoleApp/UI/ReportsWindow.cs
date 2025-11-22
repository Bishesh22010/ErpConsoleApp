using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpConsoleApp.UI
{
    public class ReportsWindow : Window
    {
        // Filter Controls
        private DateField startDateField;
        private DateField endDateField;
        private ComboBox reportTypeCombo;

        // Log Controls
        private ComboBox logModuleCombo;

        // Custom Report Controls
        private ComboBox employeeCombo;
        private List<Employee> employees = new List<Employee>();

        public ReportsWindow() : base("Generate Reports & Logs (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            KeyDown += (e) => { if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; } };

            // ========================================================
            // LEFT PANE: STANDARD REPORTS
            // ========================================================
            var stdReportFrame = new FrameView("Standard Reports")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Percent(60)
            };

            int y = 1;
            stdReportFrame.Add(new Label("Timeframe:") { X = 1, Y = y, ColorScheme = Colors.MenuScheme });

            startDateField = new DateField(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1))
            { X = 15, Y = y, Width = 12, ColorScheme = Colors.TextScheme };

            stdReportFrame.Add(startDateField);
            stdReportFrame.Add(new Label("to") { X = 29, Y = y });

            endDateField = new DateField(DateTime.Now)
            { X = 34, Y = y, Width = 12, ColorScheme = Colors.TextScheme };
            stdReportFrame.Add(endDateField);

            y += 2;
            stdReportFrame.Add(new Label("Report Type:") { X = 1, Y = y, ColorScheme = Colors.MenuScheme });
            reportTypeCombo = new ComboBox()
            { X = 15, Y = y, Width = 30, Height = 5, ColorScheme = Colors.TextScheme };
            reportTypeCombo.SetSource(new List<string> { "Employee Data", "Salary Data", "Voucher Data" });
            reportTypeCombo.SelectedItem = 0;
            stdReportFrame.Add(reportTypeCombo);

            y += 2;
            var btnGenerateStd = new Button("Generate _Standard Report")
            { X = 15, Y = y, ColorScheme = Colors.ButtonScheme };
            btnGenerateStd.Clicked += () => GenerateReport(false);
            stdReportFrame.Add(btnGenerateStd);

            // ========================================================
            // RIGHT PANE: ACTIVITY LOGS
            // ========================================================
            var logFrame = new FrameView("Activity Logs")
            {
                X = Pos.Right(stdReportFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Percent(60)
            };

            y = 1;
            logFrame.Add(new Label("Module:") { X = 1, Y = y, ColorScheme = Colors.MenuScheme });
            logModuleCombo = new ComboBox()
            { X = 10, Y = y, Width = 30, Height = 10, ColorScheme = Colors.TextScheme };
            logModuleCombo.SetSource(new List<string> {
                "All Modules", "Dashboard", "Manage Employee", "Salary", "Voucher", "Reports", "Settings", "Login"
            });
            logModuleCombo.SelectedItem = 0;
            logFrame.Add(logModuleCombo);

            y += 2;
            var btnDownloadLog = new Button("Download _Activity Log")
            { X = 10, Y = y, ColorScheme = Colors.ButtonScheme };
            btnDownloadLog.Clicked += DownloadLogs;
            logFrame.Add(btnDownloadLog);

            // ========================================================
            // BOTTOM PANE: CUSTOM REPORTS
            // ========================================================
            var customReportFrame = new FrameView("Custom Report (Single Employee)")
            {
                X = 0,
                Y = Pos.Bottom(stdReportFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };

            y = 1;
            customReportFrame.Add(new Label("Select Employee:") { X = 1, Y = y, ColorScheme = Colors.MenuScheme });
            employeeCombo = new ComboBox()
            { X = 20, Y = y, Width = 30, Height = 5, ColorScheme = Colors.TextScheme };
            customReportFrame.Add(employeeCombo);

            var btnGenerateCustom = new Button("Generate _Custom Report")
            { X = 55, Y = y, ColorScheme = Colors.ButtonScheme };
            btnGenerateCustom.Clicked += () => GenerateReport(true);
            customReportFrame.Add(btnGenerateCustom);

            // Footer
            var btnBack = new Button("_Back")
            { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ErrorScheme };
            btnBack.Clicked += () => Application.RequestStop();

            Add(stdReportFrame, logFrame, customReportFrame, btnBack);

            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    employees = db.Employees.OrderBy(e => e.Name).ToList();
                    var names = employees.Select(e => e.Name).ToList();
                    employeeCombo.SetSource(names);
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        // --- REPORT GENERATION LOGIC ---
        private void GenerateReport(bool isCustom)
        {
            if (startDateField.Date > endDateField.Date)
            {
                Program.ShowError("Error", "Start Date cannot be after End Date."); return;
            }

            string reportType = isCustom ? "Custom Report" : reportTypeCombo.Text.ToString();
            int? employeeId = null;

            if (isCustom)
            {
                if (employeeCombo.SelectedItem < 0 || employeeCombo.SelectedItem >= employees.Count)
                {
                    Program.ShowError("Error", "Select an employee."); return;
                }
                employeeId = employees[employeeCombo.SelectedItem].Id;
                reportType = $"Custom Report - {employees[employeeCombo.SelectedItem].Name}";
            }

            // Ask for Format
            var format = AskForFormat();
            if (string.IsNullOrEmpty(format)) return; // Cancelled

            try
            {
                using (var db = new AppDbContext())
                {
                    StringBuilder sb = new StringBuilder();
                    string fileName = $"{reportType.Replace(" ", "")}_{DateTime.Now:yyyyMMdd_HHmm}.{format.ToLower()}";

                    // Build Data
                    if (format == "CSV") sb.AppendLine(GetCsvHeader(reportType));
                    else sb.AppendLine(GetTextHeader(reportType));

                    // Fetch and Process Data
                    if (reportType.Contains("Employee Data"))
                    {
                        var data = db.Employees.ToList(); // Date filter doesn't apply well to static employee data usually
                        foreach (var d in data)
                        {
                            if (format == "CSV") sb.AppendLine($"{d.Id},{d.Name},{d.MobNo},{d.Salary},{d.Borrow}");
                            else sb.AppendLine($"{d.Id,-5} | {d.Name,-20} | {d.MobNo,-12} | {d.Salary,10:N2} | {d.Borrow,10:N2}");
                        }
                    }
                    else if (reportType.Contains("Salary Data") || isCustom)
                    {
                        var query = db.Salaries.Include(s => s.Employee).AsQueryable();
                        query = query.Where(s => s.PaymentDate >= startDateField.Date && s.PaymentDate <= endDateField.Date);
                        if (employeeId.HasValue) query = query.Where(s => s.EmployeeId == employeeId.Value);

                        foreach (var s in query.ToList())
                        {
                            if (format == "CSV") sb.AppendLine($"{s.PaymentDate:yyyy-MM-dd},{s.Employee.Name},{s.SalaryAmount},{s.FinalSalary}");
                            else sb.AppendLine($"{s.PaymentDate:yyyy-MM-dd} | {s.Employee.Name,-20} | Base: {s.SalaryAmount,10:N2} | Paid: {s.FinalSalary,10:N2}");
                        }
                    }
                    else if (reportType.Contains("Voucher Data"))
                    {
                        var query = db.Vouchers.Include(v => v.Employee).Where(v => v.VoucherDate >= startDateField.Date && v.VoucherDate <= endDateField.Date);
                        foreach (var v in query.ToList())
                        {
                            if (format == "CSV") sb.AppendLine($"{v.VoucherDate:yyyy-MM-dd},{v.Employee.Name},{v.Amount},{v.Reason}");
                            else sb.AppendLine($"{v.VoucherDate:yyyy-MM-dd} | {v.Employee.Name,-20} | {v.Amount,10:N2} | {v.Reason}");
                        }
                    }

                    File.WriteAllText(fileName, sb.ToString());
                    Program.ShowMessage("Success", $"Report saved to:\n{fileName}");
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void DownloadLogs()
        {
            string module = logModuleCombo.Text.ToString();
            var format = AskForFormat(); // Reuse format picker
            if (string.IsNullOrEmpty(format)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var query = db.Logs.AsQueryable();
                    if (module != "All Modules") query = query.Where(l => l.Module == module);

                    var logs = query.OrderByDescending(l => l.Timestamp).ToList();
                    if (logs.Count == 0) { Program.ShowError("Info", "No logs found."); return; }

                    StringBuilder sb = new StringBuilder();
                    string fileName = $"Logs_{module.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.{format.ToLower()}";

                    if (format == "CSV") sb.AppendLine("Timestamp,Module,Action");

                    foreach (var l in logs)
                    {
                        if (format == "CSV") sb.AppendLine($"{l.Timestamp},{l.Module},{l.Action}");
                        else sb.AppendLine($"[{l.Timestamp}] [{l.Module}] {l.Action}");
                    }

                    File.WriteAllText(fileName, sb.ToString());
                    Program.ShowMessage("Success", $"Logs saved to:\n{fileName}");
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        // Helper to ask user for format
        private string AskForFormat()
        {
            int result = MessageBox.Query("Select Format", "Choose export format:", "Excel (CSV)", "Text File", "Cancel");
            if (result == 0) return "CSV";
            if (result == 1) return "TXT";
            return null;
        }

        private string GetCsvHeader(string type)
        {
            if (type.Contains("Employee")) return "ID,Name,Mobile,Salary,Borrow";
            if (type.Contains("Salary")) return "Date,Name,Base Salary,Paid Amount";
            if (type.Contains("Voucher")) return "Date,Name,Amount,Reason";
            return "Date,Description,Amount";
        }

        private string GetTextHeader(string type)
        {
            return $"--- REPORT: {type} ---\nDate Range: {startDateField.Date:d} to {endDateField.Date:d}\n" + new string('-', 60);
        }
    }
}