using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpConsoleApp.UI
{
    public class ItemWiseReportWindow : Window
    {
        private ComboBox itemCombo;
        private DateField monthField;
        private ListView reportList;
        private ListView recentFilesList;
        private Label summaryLabel;

        private List<string> itemNames = new List<string>();
        private List<PurchaseSlip> currentSlips = new List<PurchaseSlip>();
        private List<FileInfo> recentFiles = new List<FileInfo>();

        public ItemWiseReportWindow() : base("Item-Wise Report (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Top Pane: Filters ---
            var filterFrame = new FrameView("Filter Criteria")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 6
            };

            filterFrame.Add(new Label("Select Item:") { X = 2, Y = 1 });
            itemCombo = new ComboBox()
            {
                X = 16,
                Y = 1,
                Width = 30,
                Height = 4,
                ColorScheme = Colors.TextScheme
            };

            filterFrame.Add(new Label("Select Month:") { X = 50, Y = 1 });
            monthField = new DateField(DateTime.Now)
            {
                X = 64,
                Y = 1,
                Width = 12,
                ColorScheme = Colors.TextScheme
            };

            var btnLoad = new Button("_Load Report")
            {
                X = 80,
                Y = 1,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            btnLoad.Clicked += LoadReport;

            filterFrame.Add(itemCombo, monthField, btnLoad);

            // --- Middle Container ---
            var middleContainer = new View()
            {
                X = 0,
                Y = 6,
                Width = Dim.Fill(),
                Height = Dim.Fill(4)
            };

            // Left: Report Data
            var listFrame = new FrameView("Report Preview")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(70),
                Height = Dim.Fill()
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

            // Right: Recent Files
            var recentFrame = new FrameView("Recent Item Exports")
            {
                X = Pos.Right(listFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
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
            recentFrame.Add(recentFilesList);

            middleContainer.Add(listFrame, recentFrame);

            // --- Footer: Actions ---
            var footerFrame = new FrameView("Actions")
            {
                X = 0,
                Y = Pos.AnchorEnd(4),
                Width = Dim.Fill(),
                Height = 4
            };

            summaryLabel = new Label("Total: 0.00 | Count: 0") { X = 2, Y = 0 };

            var btnExportCsv = new Button("Export _Excel") { X = 2, Y = 1, ColorScheme = Colors.ButtonScheme };
            btnExportCsv.Clicked += () => ExportData("csv");

            var btnExportTxt = new Button("Export _Text") { X = 20, Y = 1, ColorScheme = Colors.ButtonScheme };
            btnExportTxt.Clicked += () => ExportData("txt");

            var btnOpenRecent = new Button("Open _Recent") { X = 40, Y = 1, ColorScheme = Colors.ButtonScheme };
            btnOpenRecent.Clicked += OpenRecentFile;

            var btnClose = new Button("_Back") { X = Pos.AnchorEnd(10), Y = 1, ColorScheme = Colors.ErrorScheme };
            btnClose.Clicked += () => Application.RequestStop();

            footerFrame.Add(summaryLabel, btnExportCsv, btnExportTxt, btnOpenRecent, btnClose);

            Add(filterFrame, middleContainer, footerFrame);

            LoadItems();
            LoadRecentFiles();
            itemCombo.SetFocus();
        }

        private void LoadItems()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    itemNames = db.Items.OrderBy(i => i.ItemName).Select(i => i.ItemName).ToList();
                    itemCombo.SetSource(itemNames);
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void LoadReport()
        {
            string selectedItem = itemCombo.Text?.ToString().Trim() ?? "";
            DateTime date = monthField.Date;

            if (string.IsNullOrWhiteSpace(selectedItem))
            {
                Program.ShowError("Error", "Please select an item.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    currentSlips = db.PurchaseSlips
                        .Include(s => s.Party)
                        .Where(s => s.ItemName.ToLower() == selectedItem.ToLower() &&
                                    s.SlipDate.Month == date.Month &&
                                    s.SlipDate.Year == date.Year)
                        .OrderBy(s => s.SlipDate)
                        .ToList();

                    var displayList = currentSlips.Select(s =>
                        string.Format("{0:yyyy-MM-dd} | {1,-15} | {2,10:N2} | {3}",
                            s.SlipDate, s.Party.Name, s.Amount,
                            s.IsPaid ? "CLEARED" : $"PARTIAL ({s.Amount - s.PaidAmount:N0} left)")
                    ).ToList();

                    if (displayList.Count == 0) displayList.Add("No records found for this item/month.");
                    reportList.SetSource(displayList);

                    decimal total = currentSlips.Sum(s => s.Amount);
                    summaryLabel.Text = $"Total: ₹{total:N2} | Count: {currentSlips.Count}";
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void ExportData(string format)
        {
            if (currentSlips.Count == 0) { Program.ShowError("Error", "No data to export."); return; }

            string itemName = itemCombo.Text.ToString().Replace(" ", "_");
            string fileName = $"ItemReport_{itemName}_{monthField.Date:yyyy_MM}_{DateTime.Now:HHmmss}.{format}";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            try
            {
                StringBuilder sb = new StringBuilder();
                if (format == "csv")
                {
                    sb.AppendLine("Date,Party,Amount,Paid,Status");
                    foreach (var s in currentSlips)
                        sb.AppendLine($"{s.SlipDate:yyyy-MM-dd},{s.Party.Name},{s.Amount},{s.PaidAmount},{(s.IsPaid ? "CLEARED" : "PENDING")}");
                }
                else
                {
                    sb.AppendLine($"--- ITEM REPORT: {itemCombo.Text} ({monthField.Date:MM/yyyy}) ---");
                    sb.AppendLine(new string('-', 65));
                    sb.AppendLine($"{"Date",-12} | {"Party Name",-20} | {"Amount",10} | {"Status",-10}");
                    sb.AppendLine(new string('-', 65));
                    foreach (var s in currentSlips)
                        sb.AppendLine($"{s.SlipDate:yyyy-MM-dd,-12} | {s.Party.Name,-20} | {s.Amount,10:N2} | {(s.IsPaid ? "CLEARED" : "PENDING"),-10}");
                    sb.AppendLine(new string('-', 65));
                    sb.AppendLine($"TOTAL AMOUNT: ₹{currentSlips.Sum(s => s.Amount):N2}");
                }

                File.WriteAllText(fullPath, sb.ToString());
                Program.ShowMessage("Export Successful", $"Saved as: {fileName}");
                LoadRecentFiles();
            }
            catch (Exception e) { Program.ShowError("Export Failed", e.Message); }
        }

        private void LoadRecentFiles()
        {
            try
            {
                var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                recentFiles = d.GetFiles("ItemReport_*.*")
                               .OrderByDescending(f => f.CreationTime).Take(5).ToList();
                var names = recentFiles.Select(f => f.Name).ToList();
                if (names.Count == 0) names.Add("No recent item reports.");
                recentFilesList.SetSource(names);
            }
            catch { /* Ignore file errors */ }
        }

        private void OpenRecentFile()
        {
            if (recentFilesList.SelectedItem >= 0 && recentFilesList.SelectedItem < recentFiles.Count)
            {
                try
                {
                    new Process { StartInfo = new ProcessStartInfo(recentFiles[recentFilesList.SelectedItem].FullName) { UseShellExecute = true } }.Start();
                }
                catch (Exception e) { Program.ShowError("Error", e.Message); }
            }
        }
    }
}