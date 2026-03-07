using System;
using System.Collections.Generic;
using System.Linq;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    public class PaymentWindow : Window
    {
        private ComboBox partyCombo;
        private Label partyNameLabel;
        private ListView slipList;
        private List<Party> allParties = new List<Party>();
        private List<PurchaseSlip> currentSlips = new List<PurchaseSlip>();

        public PaymentWindow() : base("Make Payment (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            var searchFrame = new FrameView("Select Party") { X = 0, Y = 0, Width = Dim.Fill(), Height = 6 };

            searchFrame.Add(new Label("Party ID:") { X = 2, Y = 1 });
            partyCombo = new ComboBox() { X = 15, Y = 1, Width = 20, Height = 4, ColorScheme = Colors.TextScheme };
            partyCombo.SelectedItemChanged += OnPartySelected;
            searchFrame.Add(partyCombo);

            partyNameLabel = new Label("Name: ") { X = 38, Y = 1, ColorScheme = Colors.ResultScheme };
            searchFrame.Add(partyNameLabel);

            var listFrame = new FrameView("Purchase Slips") { X = 0, Y = 6, Width = Dim.Fill(), Height = Dim.Fill(2) };
            slipList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            listFrame.Add(slipList);

            var payButton = new Button("_Pay Selected Slip") { X = Pos.Center() - 35, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            payButton.Clicked += OnPaySlip;

            var masterClearButton = new Button("_Master Clear (All Pending)") { X = Pos.Center() - 10, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            masterClearButton.Clicked += OnMasterClear;

            var closeButton = new Button("_Back") { X = Pos.Center() + 25, Y = Pos.AnchorEnd(2), IsDefault = true, ColorScheme = Colors.ButtonScheme };
            closeButton.Clicked += () => Application.RequestStop();

            // --- NEW: App-wide Shortcut Display Pattern ---
            var shortcutsLabel = new Label("Shortcuts: [Alt+P] Pay | [Alt+M] Master Clear | [Alt+B]/[ESC] Back | [Tab] Navigate")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1), // Placed at the very bottom
                ColorScheme = Colors.ResultScheme
            };

            Add(searchFrame, listFrame, payButton, masterClearButton, closeButton, shortcutsLabel);
            LoadPartiesFromDb();
            partyCombo.SetFocus();
        }

        private void LoadPartiesFromDb()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    allParties = db.Parties.OrderBy(p => p.PartyCode).ToList();
                    partyCombo.SetSource(allParties.Select(p => p.PartyCode).ToList());
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnPartySelected(ListViewItemEventArgs args)
        {
            string code = args.Value?.ToString() ?? "";
            var party = allParties.FirstOrDefault(p => p.PartyCode == code);

            if (party != null)
            {
                partyNameLabel.Text = $"Name: {party.Name}";
                try
                {
                    using (var db = new AppDbContext())
                    {
                        currentSlips = db.PurchaseSlips.Include(s => s.Party).Where(s => s.PartyId == party.PartyId).OrderBy(s => s.SlipDate).ToList();
                        var slipDisplayList = currentSlips.Select(s => GetSlipDisplayString(s)).ToList();
                        slipList.SetSource(slipDisplayList);
                    }
                }
                catch (Exception e) { Program.ShowError("DB Error", e.Message); }
            }
            else
            {
                partyNameLabel.Text = "Name: Not Found";
                currentSlips.Clear();
                slipList.SetSource(new List<string>());
            }
        }

        private string GetSlipDisplayString(PurchaseSlip s)
        {
            string status = s.IsPaid ? "CLEARED" : (s.PaidAmount > 0 ? $"PARTIAL (₹{s.Amount - s.PaidAmount:N2} left)" : "PENDING");
            return string.Format("{0} | {1,-15} | ₹{2,10:N2} | {3}", s.SlipDate.ToString("yyyy-MM-dd"), s.ItemName, s.Amount, status);
        }

        private void OnPaySlip()
        {
            if (slipList.SelectedItem < 0 || slipList.SelectedItem >= currentSlips.Count) return;
            var selectedSlip = currentSlips[slipList.SelectedItem];
            if (selectedSlip.IsPaid) { Program.ShowMessage("Info", "This slip is already CLEARED."); return; }
            ShowPaymentDialog(selectedSlip);
        }

        private void ShowPaymentDialog(PurchaseSlip slip)
        {
            var dialog = new Dialog("Slip Payment Details", 60, 14) { ColorScheme = Colors.DialogScheme };
            decimal remaining = slip.Amount - slip.PaidAmount;

            dialog.Add(new Label($"Total Amount:      ₹{slip.Amount:N2}") { X = 2, Y = 1 });
            dialog.Add(new Label($"Already Paid:      ₹{slip.PaidAmount:N2}") { X = 2, Y = 2 });
            dialog.Add(new Label($"Remaining:         ₹{remaining:N2}") { X = 2, Y = 3 });

            dialog.Add(new Label("Enter Payment:") { X = 2, Y = 5 });
            var amountField = new TextField("") { X = 20, Y = 5, Width = 20, ColorScheme = Colors.TextScheme };

            var btnPayPartial = new Button("Pay _Amount") { X = 5, Y = 9, ColorScheme = Colors.ButtonScheme, IsDefault = true };
            btnPayPartial.Clicked += () => {
                if (decimal.TryParse(amountField.Text.ToString(), out decimal payAmt) && payAmt > 0)
                {
                    if (payAmt > remaining) { Program.ShowError("Error", "Amount exceeds balance."); return; }
                    ProcessPayment(slip.PurchaseSlipId, payAmt, false);
                    Application.RequestStop();
                }
                else { Program.ShowError("Error", "Invalid Amount"); }
            };

            var btnMarkCleared = new Button("Mark _Cleared") { X = 25, Y = 9, ColorScheme = Colors.ButtonScheme };
            btnMarkCleared.Clicked += () => {
                if (Program.ShowQuery("Confirm", "Mark as fully CLEARED?"))
                {
                    ProcessPayment(slip.PurchaseSlipId, 0, true);
                    Application.RequestStop();
                }
            };

            var btnCancel = new Button("_Cancel") { X = 45, Y = 9, ColorScheme = Colors.ButtonScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            dialog.Add(amountField, btnPayPartial, btnMarkCleared, btnCancel);
            Application.Run(dialog);
        }

        private void ProcessPayment(int slipId, decimal payAmount, bool forceClear)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var slip = db.PurchaseSlips.Find(slipId);
                    if (slip != null)
                    {
                        if (forceClear) { slip.IsPaid = true; slip.PaidAmount = slip.Amount; }
                        else
                        {
                            slip.PaidAmount += payAmount;
                            if (slip.PaidAmount >= slip.Amount) { slip.PaidAmount = slip.Amount; slip.IsPaid = true; }
                        }
                        db.SaveChanges();
                    }
                }
                // Refresh list using existing combo box text
                OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnMasterClear()
        {
            if (currentSlips.Count == 0) return;
            var slipsToClear = currentSlips.Where(s => !s.IsPaid && s.PaidAmount == 0).ToList();
            if (slipsToClear.Count == 0) return;
            if (!Program.ShowQuery("Master Clear", $"Clear {slipsToClear.Count} Pending slip(s)?")) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    foreach (var s in slipsToClear)
                    {
                        var slip = db.PurchaseSlips.Find(s.PurchaseSlipId);
                        if (slip != null) { slip.IsPaid = true; slip.PaidAmount = slip.Amount; }
                    }
                    db.SaveChanges();
                }
                OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}