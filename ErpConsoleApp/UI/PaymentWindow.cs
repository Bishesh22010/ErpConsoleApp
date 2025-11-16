using System;
using System.Collections.Generic;
using System.Linq;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    /// <_summary>
    /// Modal window for viewing and paying purchase slips.
    /// </_summary>
    public class PaymentWindow : Window
    {
        private ComboBox partyCombo;
        private ListView slipList;
        private List<string> partyNames = new List<string>();
        private List<PurchaseSlip> currentSlips = new List<PurchaseSlip>();

        public PaymentWindow() : base("Make Payment")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center();
            Y = Pos.Center() - 8;
            Width = 80;
            Height = 22;
            Modal = true;

            // --- Party Search ---
            var searchLabel = new Label("Search Party:")
            {
                X = 2,
                Y = 1
            };

            partyCombo = new ComboBox()
            {
                X = 18,
                Y = 1,
                Width = Dim.Fill(2),
                ColorScheme = Colors.TextScheme
            };
            partyCombo.SetSource(new List<string>()); // Start empty
            partyCombo.SelectedItemChanged += OnPartySelected;
            LoadPartiesFromDb(); // Load names into ComboBox

            Add(searchLabel, partyCombo);

            // --- Slips List ---
            var listFrame = new FrameView("Purchase Slips")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
                Height = 12
            };

            slipList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            listFrame.Add(slipList);
            Add(listFrame);

            // --- Buttons ---
            var payButton = new Button("_Pay Selected Slip")
            {
                X = Pos.Center() - 18,
                Y = 16,
                ColorScheme = Colors.ButtonScheme
            };
            payButton.Clicked += OnPaySlip;

            var closeButton = new Button("_Close")
            {
                X = Pos.Center() + 8,
                Y = 16,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            closeButton.Clicked += () => Application.RequestStop();

            Add(payButton, closeButton);

            partyCombo.SetFocus();
        }

        private void LoadPartiesFromDb()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    partyNames = db.Parties.OrderBy(p => p.Name).Select(p => p.Name).ToList();
                    partyCombo.SetSource(partyNames);
                }
            }
            catch (Exception e)
            {
                Program.ShowError("DB Error", $"Could not load parties:\n{e.Message}");
            }
        }

        private void OnPartySelected(ListViewItemEventArgs args)
        {
            string partyName = args.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(partyName))
            {
                currentSlips.Clear();
                slipList.SetSource(new List<string>());
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    currentSlips = db.PurchaseSlips
                        .Where(s => s.Party.Name == partyName)
                        .OrderBy(s => s.SlipDate)
                        .ToList();

                    var slipDisplayList = currentSlips.Select(s =>
                        string.Format("{0} | {1,-15} | {2,10:C} | {3}",
                            s.SlipDate.ToString("yyyy-MM-dd"),
                            s.ItemName,
                            s.Amount,
                            s.IsPaid ? "CLEARED" : "PENDING")
                    ).ToList();

                    slipList.SetSource(slipDisplayList);
                }
            }
            catch (Exception e)
            {
                Program.ShowError("DB Error", $"Could not load slips:\n{e.Message}");
            }
        }

        private void OnPaySlip()
        {
            if (slipList.SelectedItem < 0 || slipList.SelectedItem >= currentSlips.Count)
            {
                Program.ShowError("Error", "Please select a slip from the list to pay.");
                return;
            }

            var selectedSlip = currentSlips[slipList.SelectedItem];

            if (selectedSlip.IsPaid)
            {
                Program.ShowMessage("Already Paid", "This slip has already been marked as 'Cleared'.");
                return;
            }

            if (!Program.ShowQuery("Confirm Payment",
                $"Mark slip for '{selectedSlip.ItemName}' ({selectedSlip.Amount:C}) as 'Cleared'?"))
            {
                return; // User clicked "No"
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    selectedSlip.IsPaid = true;
                    db.PurchaseSlips.Update(selectedSlip);
                    db.SaveChanges();
                }

                // Refresh the list
                OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
            }
            catch (Exception e)
            {
                Program.ShowError("Database Error", $"Could not update slip:\n{e.Message}");
            }
        }
    }
}