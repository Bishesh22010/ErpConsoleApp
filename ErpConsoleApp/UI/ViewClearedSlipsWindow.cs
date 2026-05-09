using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ViewClearedSlipsWindow : Window
    {
        public ViewClearedSlipsWindow(Party party) : base($"Cleared Slips for: {party.Name} (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            var listFrame = new FrameView("Cleared Slip History")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(3)
            };

            var listView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            listView.SetSource(new List<string>());
            listFrame.Add(listView);

            var displayLines = new List<string>();
            decimal grandTotal = 0;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Fetch only slips that belong to this party and are completely paid/cleared
                    var slips = db.PurchaseSlips
                        .Where(s => s.PartyId == party.PartyId && s.IsPaid)
                        .OrderBy(s => s.SlipDate)
                        .ThenBy(s => s.SlipNumber)
                        .ToList();

                    if (slips.Count == 0)
                    {
                        displayLines.Add("No cleared slips found for this party.");
                    }
                    else
                    {
                        var groupedSlips = slips.GroupBy(s => s.SlipNumber);

                        foreach (var group in groupedSlips)
                        {
                            var firstItem = group.First();

                            displayLines.Add(" ");
                            displayLines.Add($"=== SLIP NO: {group.Key} | DATE: {firstItem.SlipDate:dd-MM-yyyy} | STATUS: CLEARED ===");
                            displayLines.Add(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4} | {4,6} | {5,8}", "SR NO", "CODE", "NAME", "QTY", "PRICE", "TOTAL"));
                            displayLines.Add(new string('-', 60));

                            int srNo = 1;
                            decimal slipTotal = 0;

                            foreach (var item in group)
                            {
                                string shortName = item.ItemName.Length > 15 ? item.ItemName.Substring(0, 12) + "..." : item.ItemName;
                                string code = (item.ItemCode ?? "").Length > 6 ? item.ItemCode.Substring(0, 6) : (item.ItemCode ?? "");

                                displayLines.Add(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4:0.##} | {4,6:0.##} | {5,8:0.##}",
                                    srNo++, code, shortName, item.Quantity, item.UnitPrice, item.Amount));

                                slipTotal += item.Amount;
                            }

                            displayLines.Add(new string('-', 60));
                            displayLines.Add(string.Format("SLIP #{0} TOTAL: ₹{1:N2}", group.Key, slipTotal));
                            displayLines.Add(" ");

                            grandTotal += slipTotal;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Program.ShowError("DB Error", e.Message);
            }

            listView.SetSource(displayLines);

            // --- Bottom Container for Grand Total and Back Button ---
            var footerFrame = new FrameView("")
            {
                X = 0,
                Y = Pos.AnchorEnd(3),
                Width = Dim.Fill(),
                Height = 3,
                Border = new Border() { BorderStyle = BorderStyle.None }
            };

            var grandTotalLabel = new Label($"GRAND TOTAL OF CLEARED SLIPS: ₹{grandTotal:N2}")
            {
                X = 2,
                Y = 0,
                ColorScheme = Colors.ResultScheme
            };

            var btnBack = new Button("_Back")
            {
                X = Pos.AnchorEnd(10),
                Y = 0,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            btnBack.Clicked += () => Application.RequestStop();

            footerFrame.Add(grandTotalLabel, btnBack);
            Add(listFrame, footerFrame);
        }
    }
}