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
    public class MonthlyReportWindow : Window
    {
        private DateField monthField;
        private ListView reportList;
        private Label summaryLabel;
        private List<PurchaseSlip> currentSlips = new List<PurchaseSlip>();

        public MonthlyReportWindow() : base("Monthly Report (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Top Pane: Filter ---
            var filterFrame = new FrameView("Select Month")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 5
            };

            filterFrame.Add(new Label("Select Date:") { X = 2, Y = 1 });

            // We use a DateField, but we'll only care about the Month and Year
            monthField = new DateField(DateTime.Now)
            {
                X = 15,
                Y = 1,
                Width = 20,
                ColorScheme = Colors.TextScheme
            };

            var btnLoad = new Button("_Load Report")
            {
                X = 40,
                Y = 1,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            btnLoad.Clicked += LoadReport;

            filterFrame.Add(monthField, btnLoad);

            // --- Middle Pane: List ---
            var listFrame = new FrameView("Operations")
            {
                X = 0,
                Y = 5,
                Width = Dim.Fill(),
                Height = Dim.Fill(4) // Leave room for footer
            };

            reportList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            listFrame.Add(reportList);

            // --- Footer: Summary & Exports ---
            var footerFrame = new FrameView("Actions")
            {
                X = 0,
                Y = Pos.AnchorEnd(4),
                Width = Dim.Fill(),
                Height = 4
            };

            summaryLabel = new Label("Total: 0.00 | Count: 0")
            {
                X = 2,
                Y = 0,
                ColorScheme = Colors.WindowScheme
            };

            var btnExportCsv = new Button("Export to _Excel (CSV)")
            {
                X = 2,
                Y = 1,
                ColorScheme = Colors.ButtonScheme
            };
            btnExportCsv.Clicked += () => ExportData("csv");

            var btnExportTxt = new Button("Export to _Text")
            {
                X = 30,
                Y = 1,
                ColorScheme = Colors.ButtonScheme
            };
            btnExportTxt.Clicked += () => ExportData("txt");

            var btnClose = new Button("_Back")
            {
                X = Pos.AnchorEnd(10),
                Y = 1,
                ColorScheme = Colors.ErrorScheme
            };
            btnClose.Clicked += () => Application.RequestStop();

            footerFrame.Add(summaryLabel, btnExportCsv, btnExportTxt, btnClose);

            Add(filterFrame, listFrame, footerFrame);

            // Load initially
            LoadReport();
        }

        private void LoadReport()
        {
            DateTime selectedDate = monthField.Date;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Filter by Month and Year
                    currentSlips = db.PurchaseSlips
                        .Include(s => s.Party)
                        .Where(s => s.SlipDate.Month == selectedDate.Month &&
                                    s.SlipDate.Year == selectedDate.Year)
                        .OrderBy(s => s.SlipDate)
                        .ToList();

                    // Update List View
                    var displayList = currentSlips.Select(s =>
                        string.Format("{0:yyyy-MM-dd} | {1,-15} | {2,-15} | {3,10:N2} | {4}",
                            s.SlipDate,
                            s.Party.Name,
                            s.ItemName,
                            s.Amount,
                            s.IsPaid ? "CLEARED" : "PENDING")
                    ).ToList();

                    if (displayList.Count == 0) displayList.Add("No records found for this month.");
                    reportList.SetSource(displayList);

                    // Update Summary
                    decimal total = currentSlips.Sum(s => s.Amount);
                    summaryLabel.Text = $"Total: {total:C} | Records: {currentSlips.Count}";
                }
            }
            catch (Exception e)
            {
                Program.ShowError("Database Error", e.Message);
            }
        }

        private void ExportData(string format)
        {
            if (currentSlips.Count == 0)
            {
                Program.ShowError("Error", "No data to export.");
                return;
            }

            string fileName = $"Report_{monthField.Date:yyyy_MM}_{DateTime.Now:HHmmss}.{format}";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            try
            {
                StringBuilder sb = new StringBuilder();

                if (format == "csv")
                {
                    // CSV Header
                    sb.AppendLine("Date,Party Name,Item Name,Amount,Status,Paid Amount");
                    foreach (var s in currentSlips)
                    {
                        sb.AppendLine($"{s.SlipDate:yyyy-MM-dd},{s.Party.Name},{s.ItemName},{s.Amount},{(s.IsPaid ? "CLEARED" : "PENDING")},{s.PaidAmount}");
                    }
                }
                else // Text format
                {
                    sb.AppendLine($"--- MONTHLY REPORT: {monthField.Date:MMMM yyyy} ---");
                    sb.AppendLine(new string('-', 80));
                    sb.AppendLine($"{"Date",-12} | {"Party",-20} | {"Item",-20} | {"Amount",10} | {"Status",-10}");
                    sb.AppendLine(new string('-', 80));

                    foreach (var s in currentSlips)
                    {
                        sb.AppendLine($"{s.SlipDate:yyyy-MM-dd,-12} | {s.Party.Name,-20} | {s.ItemName,-20} | {s.Amount,10:N2} | {(s.IsPaid ? "CLEARED" : "PENDING"),-10}");
                    }

                    sb.AppendLine(new string('-', 80));
                    sb.AppendLine($"TOTAL AMOUNT: {currentSlips.Sum(s => s.Amount):C}");
                }

                File.WriteAllText(fullPath, sb.ToString());
                Program.ShowMessage("Export Successful", $"File saved to:\n{fileName}\n\n(Check your app folder)");
            }
            catch (Exception e)
            {
                Program.ShowError("Export Failed", e.Message);
            }
        }
    }
}