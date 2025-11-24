using System;
using System.Collections.Generic;
using System.Diagnostics; // For opening files
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
        // Generation Controls
        private DateField startDateField;
        private DateField endDateField;
        private ComboBox reportTypeCombo;
        private ComboBox employeeCombo;
        private ComboBox logModuleCombo;

        // Recent Files Controls
        private ListView recentFilesList;
        private List<FileInfo> recentFiles = new List<FileInfo>();
        private List<Employee> employees = new List<Employee>();

        public ReportsWindow() : base("Reports Center (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            KeyDown += (e) => { if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; } };

            // ========================================================
            // LEFT PANE: GENERATION CONTROLS (65%)
            // ========================================================
            var leftFrame = new FrameView("Generate Reports")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(65),
                Height = Dim.Fill(1)
            };

            // --- Section 1: Standard Reports ---
            leftFrame.Add(new Label("--- Standard Reports ---") { X = 1, Y = 0, ColorScheme = Colors.MenuScheme });

            leftFrame.Add(new Label("Date Range:") { X = 1, Y = 2 });
            startDateField = new DateField(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1))
            { X = 15, Y = 2, Width = 12, ColorScheme = Colors.TextScheme };
            leftFrame.Add(startDateField);

            leftFrame.Add(new Label("to") { X = 30, Y = 2 });
            endDateField = new DateField(DateTime.Now)
            { X = 35, Y = 2, Width = 12, ColorScheme = Colors.TextScheme };
            leftFrame.Add(endDateField);

            leftFrame.Add(new Label("Type:") { X = 1, Y = 4 });
            reportTypeCombo = new ComboBox()
            { X = 15, Y = 4, Width = 30, Height = 4, ColorScheme = Colors.TextScheme };
            reportTypeCombo.SetSource(new List<string> { "Employee Data", "Salary Data", "Voucher Data" });
            reportTypeCombo.SelectedItem = 0;
            leftFrame.Add(reportTypeCombo);

            var btnStd = new Button("Generate _Standard") { X = 50, Y = 4, ColorScheme = Colors.ButtonScheme };
            btnStd.Clicked += () => GenerateReport(false);
            leftFrame.Add(btnStd);

            // --- Section 2: Custom Reports ---
            leftFrame.Add(new Label("--- Custom Employee Report ---") { X = 1, Y = 8, ColorScheme = Colors.MenuScheme });

            leftFrame.Add(new Label("Employee:") { X = 1, Y = 10 });
            employeeCombo = new ComboBox()
            { X = 15, Y = 10, Width = 30, Height = 4, ColorScheme = Colors.TextScheme };
            leftFrame.Add(employeeCombo);

            var btnCustom = new Button("Generate _Custom") { X = 50, Y = 10, ColorScheme = Colors.ButtonScheme };
            btnCustom.Clicked += () => GenerateReport(true);
            leftFrame.Add(btnCustom);

            // --- Section 3: Logs ---
            /*leftFrame.Add(new Label("--- System Logs ---") { X = 1, Y = 14, ColorScheme = Colors.MenuScheme });

            leftFrame.Add(new Label("Module:") { X = 1, Y = 16 });
            logModuleCombo = new ComboBox()
            { X = 15, Y = 16, Width = 30, Height = 4, ColorScheme = Colors.TextScheme };
            logModuleCombo.SetSource(new List<string> {
                "All Modules", "Dashboard", "Manage Employee", "Salary", "Voucher", "Reports", "Settings", "Login"
            });
            logModuleCombo.SelectedItem = 0;
            leftFrame.Add(logModuleCombo);*/

           /* var btnLog = new Button("Download _Logs") { X = 50, Y = 16, ColorScheme = Colors.ButtonScheme };
            btnLog.Clicked += DownloadLogs;
            leftFrame.Add(btnLog);*/


            // ========================================================
            // RIGHT PANE: RECENT FILES (35%)
            // ========================================================
            var rightFrame = new FrameView("Recent Reports")
            {
                X = Pos.Right(leftFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };

            recentFilesList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            recentFilesList.OpenSelectedItem += (e) => OpenRecentFile();
            rightFrame.Add(recentFilesList);

            // --- Footer ---
            var btnOpen = new Button("Open _Selected File")
            { X = Pos.Center() - 20, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnOpen.Clicked += OpenRecentFile;

            var btnBack = new Button("_Back")
            { X = Pos.Center() + 20, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ErrorScheme };
            btnBack.Clicked += () => Application.RequestStop();

            Add(leftFrame, rightFrame, btnOpen, btnBack);

            LoadEmployees();
            LoadRecentFiles();
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

        private void LoadRecentFiles()
        {
            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo d = new DirectoryInfo(appPath);

                // Find files starting with "Report_", "PartyReport_", or "Logs_"
                recentFiles = d.GetFiles("*_*.*")
                               .Where(f => (f.Name.StartsWith("Report_") || f.Name.StartsWith("PartyReport_") || f.Name.StartsWith("Logs_"))
                                           && (f.Extension.ToLower() == ".csv" || f.Extension.ToLower() == ".txt"))
                               .OrderByDescending(f => f.CreationTime)
                               .Take(15)
                               .ToList();

                var fileNames = recentFiles.Select(f => f.Name).ToList();
                if (fileNames.Count == 0) fileNames.Add("No recent reports found.");

                recentFilesList.SetSource(fileNames);
            }
            catch { /* Ignore errors if folder access is denied */ }
        }

        private void OpenRecentFile()
        {
            if (recentFilesList.SelectedItem < 0 || recentFilesList.SelectedItem >= recentFiles.Count)
            {
                Program.ShowError("Error", "Select a file to open."); return;
            }

            var file = recentFiles[recentFilesList.SelectedItem];
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(file.FullName) { UseShellExecute = true }
                }.Start();
            }
            catch (Exception e) { Program.ShowError("Error", $"Could not open file:\n{e.Message}"); }
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

            var format = AskForFormat();
            if (string.IsNullOrEmpty(format)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    StringBuilder sb = new StringBuilder();
                    string fileName = $"Report_{reportType.Replace(" ", "")}_{DateTime.Now:yyyyMMdd_HHmm}.{format.ToLower()}";
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    if (format == "CSV") sb.AppendLine(GetCsvHeader(reportType));
                    else sb.AppendLine(GetTextHeader(reportType));

                    // Fetch and Process Data
                    bool hasData = false;
                    if (reportType.Contains("Employee Data"))
                    {
                        var data = db.Employees.ToList();
                        if (data.Any()) hasData = true;
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

                        var list = query.ToList();
                        if (list.Any()) hasData = true;
                        foreach (var s in list)
                        {
                            if (format == "CSV") sb.AppendLine($"{s.PaymentDate:yyyy-MM-dd},{s.Employee.Name},{s.SalaryAmount},{s.FinalSalary}");
                            else sb.AppendLine($"{s.PaymentDate:yyyy-MM-dd} | {s.Employee.Name,-20} | Base: {s.SalaryAmount,10:N2} | Paid: {s.FinalSalary,10:N2}");
                        }
                    }
                    else if (reportType.Contains("Voucher Data"))
                    {
                        var query = db.Vouchers.Include(v => v.Employee).Where(v => v.VoucherDate >= startDateField.Date && v.VoucherDate <= endDateField.Date);
                        var list = query.ToList();
                        if (list.Any()) hasData = true;
                        foreach (var v in list)
                        {
                            if (format == "CSV") sb.AppendLine($"{v.VoucherDate:yyyy-MM-dd},{v.Employee.Name},{v.Amount},{v.Reason}");
                            else sb.AppendLine($"{v.VoucherDate:yyyy-MM-dd} | {v.Employee.Name,-20} | {v.Amount,10:N2} | {v.Reason}");
                        }
                    }

                    if (!hasData) { Program.ShowError("Info", "No data found for this period."); return; }

                    File.WriteAllText(fullPath, sb.ToString());
                    Program.ShowMessage("Success", $"Saved: {fileName}");
                    LoadRecentFiles(); // Refresh list
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }
        /*
        private void DownloadLogs()
        {
            string module = logModuleCombo.Text.ToString();
            var format = AskForFormat();
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
                    string fileName = $"Logs_{module.Replace(" ", "")}_{DateTime.Now:yyyyMMdd_HHmm}.{format.ToLower()}";
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    if (format == "CSV") sb.AppendLine("Timestamp,Module,Action");

                    foreach (var l in logs)
                    {
                        if (format == "CSV") sb.AppendLine($"{l.Timestamp},{l.Module},{l.Action}");
                        else sb.AppendLine($"[{l.Timestamp}] [{l.Module}] {l.Action}");
                    }

                    File.WriteAllText(fullPath, sb.ToString());
                    Program.ShowMessage("Success", $"Logs saved: {fileName}");
                    LoadRecentFiles(); // Refresh list
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }*/

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