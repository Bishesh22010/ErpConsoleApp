using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ViewPartySlipsWindow : Window
    {
        public ViewPartySlipsWindow(Party party) : base($"All Generated Slips for: {party.Name} (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            var listFrame = new FrameView("Slip History")
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
                    var slips = db.PurchaseSlips
                        .Where(s => s.PartyId == party.PartyId)
                        .OrderBy(s => s.SlipDate)
                        .ThenBy(s => s.SlipNumber)
                        .ToList();

                    if (slips.Count == 0)
                    {
                        displayLines.Add("No slips have been generated for this party yet.");
                    }
                    else
                    {
                        // Group all the individual items together by their shared Slip ID
                        var groupedSlips = slips.GroupBy(s => s.SlipNumber);

                        foreach (var group in groupedSlips)
                        {
                            var firstItem = group.First();

                            // FIX: Using " " instead of "" to prevent Terminal.Gui ArgumentException crash
                            displayLines.Add(" ");
                            displayLines.Add($"=== SLIP NO: {group.Key} | DATE: {firstItem.SlipDate:dd-MM-yyyy} ===");
                            displayLines.Add(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4} | {4,-4} | {5,6} | {6,8}", "SR NO", "CODE", "NAME", "QTY", "TYPE", "PRICE", "TOTAL"));
                            displayLines.Add(new string('-', 65));

                            int srNo = 1;
                            decimal slipTotal = 0;

                            foreach (var item in group)
                            {
                                string shortName = item.ItemName.Length > 15 ? item.ItemName.Substring(0, 12) + "..." : item.ItemName;
                                string code = (item.ItemCode ?? "").Length > 6 ? item.ItemCode.Substring(0, 6) : (item.ItemCode ?? "");

                                displayLines.Add(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4:0.##} | {4,-4} | {5,6:0.##} | {6,8:0.##}",
                                    srNo++, code, shortName, item.Quantity, item.QtyType ?? "-", item.UnitPrice, item.Amount));

                                slipTotal += item.Amount;
                            }

                            displayLines.Add(new string('-', 65));
                            displayLines.Add(string.Format("SLIP #{0} TOTAL: ₹{1:N2}", group.Key, slipTotal));

                            // FIX: Using " " instead of "" to prevent Terminal.Gui ArgumentException crash
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

            var grandTotalLabel = new Label($"GRAND TOTAL OF ALL SLIPS: ₹{grandTotal:N2}")
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