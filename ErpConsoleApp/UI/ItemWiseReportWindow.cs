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
        private Label itemNameLabel;
        private DateField monthField;
        private ListView reportList;
        private ListView recentFilesList;
        private Label summaryLabel;

        private List<Item> allItems = new List<Item>();
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

            filterFrame.Add(new Label("Item Code:") { X = 2, Y = 1 });
            itemCombo = new ComboBox() { X = 15, Y = 1, Width = 20, Height = 4, ColorScheme = Colors.TextScheme };
            itemCombo.SelectedItemChanged += (e) => {
                var i = allItems.FirstOrDefault(x => x.ItemCode == (e.Value?.ToString() ?? ""));
                itemNameLabel.Text = i != null ? $"Name: {i.ItemName}" : "Name: Not Found";
            };
            filterFrame.Add(itemCombo);

            itemNameLabel = new Label("Name: ") { X = 38, Y = 1, ColorScheme = Colors.ResultScheme };
            filterFrame.Add(itemNameLabel);

            filterFrame.Add(new Label("Select Month:") { X = 2, Y = 3 });
            monthField = new DateField(DateTime.Now)
            {
                X = 15,
                Y = 3,
                Width = 12,
                ColorScheme = Colors.TextScheme
            };
            filterFrame.Add(monthField);

            var btnLoad = new Button("_Load Report")
            {
                X = 38,
                Y = 3,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            btnLoad.Clicked += LoadReport;
            filterFrame.Add(btnLoad);

            // --- Middle Container ---
            var middleContainer = new View()
            {
                X = 0,
                Y = 6,
                Width = Dim.Fill(),
                Height = Dim.Fill(5) // Adjusted to Dim.Fill(5) for larger footer
            };

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
                Y = Pos.AnchorEnd(5),
                Width = Dim.Fill(),
                Height = 5 // Increased height for Shortcuts
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

            // --- NEW: App-wide Shortcut Display Pattern ---
            var shortcutsLabel = new Label("Shortcuts: [Alt+L] Load | [Alt+E] Excel | [Alt+T] Text | [Alt+O] Open | [Alt+B]/[ESC] Back | [Tab] Navigate")
            {
                X = Pos.Center(),
                Y = 2, // FIXED: Changed from 3 to 2 so it isn't hidden behind the border
                ColorScheme = Colors.ResultScheme
            };

            footerFrame.Add(summaryLabel, btnExportCsv, btnExportTxt, btnOpenRecent, btnClose, shortcutsLabel);

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
                    allItems = db.Items.OrderBy(i => i.ItemCode).ToList();
                    itemCombo.SetSource(allItems.Select(i => i.ItemCode).ToList());
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void LoadReport()
        {
            string itemCode = itemCombo.Text?.ToString().Trim() ?? "";
            DateTime date = monthField.Date;

            var item = allItems.FirstOrDefault(i => i.ItemCode == itemCode);
            if (item == null)
            {
                Program.ShowError("Error", "Please select a valid Item Code.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    currentSlips = db.PurchaseSlips
                        .Include(s => s.Party)
                        .Where(s => s.ItemName == item.ItemName &&
                                    s.SlipDate.Month == date.Month &&
                                    s.SlipDate.Year == date.Year)
                        .OrderBy(s => s.SlipDate)
                        .ToList();

                    var displayList = currentSlips.Select(s =>
                        string.Format("{0:yyyy-MM-dd} | {1,-15} | ₹{2,10:N2} | {3}",
                            s.SlipDate, s.Party.Name, s.Amount,
                            s.IsPaid ? "CLEARED" : $"PARTIAL (₹{s.Amount - s.PaidAmount:N2} left)")
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

            string itemName = allItems.FirstOrDefault(i => i.ItemCode == itemCombo.Text.ToString())?.ItemName.Replace(" ", "_") ?? "Unknown";
            string fileName = $"ItemReport_{itemName}_{monthField.Date:yyyy_MM}_{DateTime.Now:HHmmss}.{format}";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            try
            {
                StringBuilder sb = new StringBuilder();
                if (format == "csv")
                {
                    sb.AppendLine("Date,Party,Amount,Status");
                    foreach (var s in currentSlips)
                        sb.AppendLine($"{s.SlipDate:yyyy-MM-dd},{s.Party.Name},{s.Amount},{(s.IsPaid ? "CLEARED" : "PENDING")}");
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