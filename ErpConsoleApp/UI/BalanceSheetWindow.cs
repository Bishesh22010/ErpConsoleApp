using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// Financial Balance Sheet Window.
    /// Summarizes total payables (Parties) and total receivables (Employees).
    /// </summary>
    public class BalanceSheetWindow : Window
    {
        private ListView partyPayableList;
        private ListView employeeReceivableList;
        private Label totalPayableLabel;
        private Label totalReceivableLabel;
        private Label netBalanceLabel;

        public BalanceSheetWindow() : base("Financial Balance Sheet (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Top Header ---
            var header = new Label("BUSINESS FINANCIAL SUMMARY")
            {
                X = Pos.Center(),
                Y = 1,
                ColorScheme = Colors.MenuScheme
            };
            Add(header);

            // --- Left Pane: Payables (Liabilities) ---
            var leftFrame = new FrameView("Party Payables (We Owe Them)")
            {
                X = 2,
                Y = 3,
                Width = Dim.Percent(48),
                Height = Dim.Fill(6)
            };
            partyPayableList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            leftFrame.Add(partyPayableList);

            // --- Right Pane: Receivables (Assets) ---
            var rightFrame = new FrameView("Employee Receivables (They Owe Us)")
            {
                X = Pos.Percent(52),
                Y = 3,
                Width = Dim.Fill(2),
                Height = Dim.Fill(6)
            };
            employeeReceivableList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            rightFrame.Add(employeeReceivableList);

            // --- Bottom Summary Pane ---
            var summaryFrame = new FrameView("Financial Totals")
            {
                X = 2,
                Y = Pos.AnchorEnd(6),
                Width = Dim.Fill(2),
                Height = 5
            };

            totalPayableLabel = new Label("Total Payables: ₹0.00") { X = 2, Y = 0, ColorScheme = Colors.ErrorScheme };
            totalReceivableLabel = new Label("Total Receivables: ₹0.00") { X = 2, Y = 1, ColorScheme = Colors.ButtonScheme };
            netBalanceLabel = new Label("NET POSITION: ₹0.00") { X = Pos.AnchorEnd(40), Y = 1, ColorScheme = Colors.ResultScheme };

            summaryFrame.Add(totalPayableLabel, totalReceivableLabel, netBalanceLabel);

            // --- Back Button ---
            var btnBack = new Button("_Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                ColorScheme = Colors.ButtonScheme
            };
            btnBack.Clicked += () => Application.RequestStop();

            Add(leftFrame, rightFrame, summaryFrame, btnBack);

            LoadBalanceSheet();
        }

        private void LoadBalanceSheet()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. Calculate Party Payables
                    // We use AsEnumerable() to fetch the raw data first, 
                    // then perform the math and grouping in memory to avoid SQLite translation errors.
                    var rawSlips = db.PurchaseSlips
                        .Include(s => s.Party)
                        .Where(s => !s.IsPaid)
                        .AsEnumerable()
                        .ToList();

                    var payablesData = rawSlips
                        .GroupBy(s => s.Party?.Name ?? "Unknown")
                        .Select(g => new {
                            Name = g.Key,
                            Balance = g.Sum(s => s.Amount - s.PaidAmount)
                        })
                        .OrderByDescending(x => x.Balance)
                        .ToList();

                    var payableDisplay = payablesData.Select(p =>
                        string.Format("{0,-25} | ₹{1,12:N2}", p.Name, p.Balance)
                    ).ToList();
                    if (payableDisplay.Count == 0) payableDisplay.Add("No outstanding payables.");
                    partyPayableList.SetSource(payableDisplay);

                    // 2. Calculate Employee Receivables (Borrowings)
                    var receivablesData = db.Employees
                        .AsEnumerable() // Force client-side evaluation for SQLite
                        .Where(e => e.Borrow > 0)
                        .OrderByDescending(e => e.Borrow)
                        .ToList();

                    var receivableDisplay = receivablesData.Select(e =>
                        string.Format("{0,-25} | ₹{1,12:N2}", e.Name, e.Borrow)
                    ).ToList();
                    if (receivableDisplay.Count == 0) receivableDisplay.Add("No outstanding receivables.");
                    employeeReceivableList.SetSource(receivableDisplay);

                    // 3. Summarize
                    decimal totalPayable = payablesData.Sum(x => x.Balance);
                    decimal totalReceivable = receivablesData.Sum(x => x.Borrow);
                    decimal netPosition = totalReceivable - totalPayable;

                    totalPayableLabel.Text = $"Total Payables:    ₹{totalPayable:N2}";
                    totalReceivableLabel.Text = $"Total Receivables: ₹{totalReceivable:N2}";

                    if (netPosition < 0)
                        netBalanceLabel.Text = $"NET POSITION (DEBT): ₹{Math.Abs(netPosition):N2}";
                    else
                        netBalanceLabel.Text = $"NET POSITION (CREDIT): ₹{netPosition:N2}";
                }
            }
            catch (Exception e)
            {
                // Detailed error reporting
                Program.ShowError("DB Error", "Failed to calculate balance sheet: " + e.Message);
            }
        }
    }
}