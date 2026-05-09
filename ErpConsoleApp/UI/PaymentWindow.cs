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
        private TextField slipNoField;
        private Label partyNameLabel;
        private Label partyPhoneLabel;
        private Label partyGstLabel;
        private ListView slipList;
        private Button btnViewCleared;

        private List<Party> allParties = new List<Party>();
        private List<SlipGroup> currentSlipGroups = new List<SlipGroup>();
        private List<SlipGroup> lineToGroupMap = new List<SlipGroup>();

        private class SlipGroup
        {
            public int SlipNumber { get; set; }
            public DateTime SlipDate { get; set; }
            public List<PurchaseSlip> Items { get; set; }
            public decimal TotalAmount => Items?.Sum(i => i.Amount) ?? 0;
            public decimal TotalPaid => Items?.Sum(i => i.PaidAmount) ?? 0;
            public decimal Remaining => TotalAmount - TotalPaid;
            public bool IsPaid => Items != null && Items.Count > 0 && Items.All(i => i.IsPaid);
        }

        public PaymentWindow() : base("Make Payment (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            var searchFrame = new FrameView("Select Party & Slip") { X = 0, Y = 0, Width = Dim.Fill(), Height = 8 };

            searchFrame.Add(new Label("Party ID:") { X = 2, Y = 1 });
            partyCombo = new ComboBox() { X = 15, Y = 1, Width = 20, Height = 4, ColorScheme = Colors.TextScheme };
            partyCombo.SelectedItemChanged += OnPartySelected;
            searchFrame.Add(partyCombo);

            searchFrame.Add(new Label("Slip No:") { X = 2, Y = 3 });
            slipNoField = new TextField("") { X = 15, Y = 3, Width = 20, ColorScheme = Colors.TextScheme };
            searchFrame.Add(slipNoField);

            partyNameLabel = new Label("Name:  ") { X = 42, Y = 1, ColorScheme = Colors.ResultScheme };
            partyPhoneLabel = new Label("Phone: ") { X = 42, Y = 3, ColorScheme = Colors.ResultScheme };
            partyGstLabel = new Label("GST:   ") { X = 42, Y = 5, ColorScheme = Colors.ResultScheme };
            searchFrame.Add(partyNameLabel, partyPhoneLabel, partyGstLabel);

            var listFrame = new FrameView("Pending Purchase Slips") { X = 0, Y = 8, Width = Dim.Fill(), Height = Dim.Fill(2) };

            slipList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            slipList.SetSource(new List<string>());
            slipList.SelectedItemChanged += (e) => {
                if (e.Item >= 0 && e.Item < lineToGroupMap.Count)
                {
                    var grp = lineToGroupMap[e.Item];
                    if (grp != null) { slipNoField.Text = grp.SlipNumber.ToString(); }
                }
            };
            listFrame.Add(slipList);

            // --- Bottom Frame: Actions ---
            var btnPay = new Button("_Pay Slip") { X = 1, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnPay.Clicked += OnPaySlip;

            var btnEditStatus = new Button("_Edit Status") { X = Pos.Right(btnPay) + 1, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnEditStatus.Clicked += OnEditStatus;

            var btnMasterClear = new Button("_Master Clear") { X = Pos.Right(btnEditStatus) + 1, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnMasterClear.Clicked += OnMasterClear;

            // --- NEW UNDO BUTTON ---
            var btnUndo = new Button("_Undo") { X = Pos.Right(btnMasterClear) + 1, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnUndo.Clicked += UndoManager.Undo;

            btnViewCleared = new Button("_View Cleared") { X = Pos.Right(btnUndo) + 1, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnViewCleared.Clicked += OnViewClearedSlips;

            var btnClose = new Button("_Back") { X = Pos.Right(btnViewCleared) + 1, Y = Pos.AnchorEnd(2), IsDefault = true, ColorScheme = Colors.ErrorScheme };
            btnClose.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+P] Pay | [Alt+E] Edit | [Alt+M] Clear All | [Ctrl+Z] Undo | [Alt+V] View | [Alt+B] Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                ColorScheme = Colors.ResultScheme
            };

            Add(searchFrame, listFrame, btnPay, btnEditStatus, btnMasterClear, btnUndo, btnViewCleared, btnClose, shortcutsLabel);
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
                partyNameLabel.Text = $"Name:  {party.Name}";
                partyPhoneLabel.Text = $"Phone: {party.PhoneNumber ?? "N/A"}";
                partyGstLabel.Text = $"GST:   {party.GstNumber ?? "N/A"}";
                slipNoField.Text = "";

                try
                {
                    using (var db = new AppDbContext())
                    {
                        currentSlipGroups = db.PurchaseSlips.Include(s => s.Party).Where(s => s.PartyId == party.PartyId).ToList()
                            .GroupBy(s => s.SlipNumber).Select(g => new SlipGroup { SlipNumber = g.Key, SlipDate = g.First().SlipDate, Items = g.ToList() })
                            .OrderBy(g => g.SlipDate).ThenBy(g => g.SlipNumber).ToList();
                        RefreshListDisplay();
                    }
                }
                catch (Exception e) { Program.ShowError("DB Error", e.Message); }
            }
            else
            {
                partyNameLabel.Text = "Name:  Not Found";
                partyPhoneLabel.Text = "Phone: "; partyGstLabel.Text = "GST:   ";
                slipNoField.Text = ""; currentSlipGroups.Clear(); lineToGroupMap.Clear(); slipList.SetSource(new List<string>());
            }
        }

        private void OnViewClearedSlips()
        {
            string code = partyCombo.Text?.ToString() ?? "";
            var party = allParties.FirstOrDefault(p => p.PartyCode == code);
            if (party == null) { MessageBox.Query("Error", "Please select a valid Party ID first.", "Ok"); return; }
            Program.OpenModal(new ViewClearedSlipsWindow(party));
            OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
        }

        private void RefreshListDisplay()
        {
            var displayLines = new List<string>(); lineToGroupMap.Clear();
            var groupsToDisplay = currentSlipGroups.Where(g => !g.IsPaid).ToList();

            if (groupsToDisplay.Count == 0)
            {
                displayLines.Add("No PENDING slips found for this party."); lineToGroupMap.Add(null);
            }
            else
            {
                foreach (var group in groupsToDisplay)
                {
                    string status = group.TotalPaid > 0 ? $"PARTIAL (₹{group.Remaining:N2} left)" : "PENDING";
                    Action<string> addLine = (text) => { displayLines.Add(text); lineToGroupMap.Add(group); };

                    addLine(" "); addLine($"=== SLIP NO: {group.SlipNumber} | DATE: {group.SlipDate:dd-MM-yyyy} | STATUS: {status} ===");
                    addLine(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4} | {4,6} | {5,8}", "SR NO", "CODE", "NAME", "QTY", "PRICE", "TOTAL"));
                    addLine(new string('-', 60));

                    int srNo = 1;
                    foreach (var item in group.Items)
                    {
                        string shortName = item.ItemName.Length > 15 ? item.ItemName.Substring(0, 12) + "..." : item.ItemName;
                        string code = (item.ItemCode ?? "").Length > 6 ? item.ItemCode.Substring(0, 6) : (item.ItemCode ?? "");
                        addLine(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4:0.##} | {4,6:0.##} | {5,8:0.##}", srNo++, code, shortName, item.Quantity, item.UnitPrice, item.Amount));
                    }
                    addLine(new string('-', 60)); addLine(string.Format("SLIP #{0} TOTAL: ₹{1:N2}", group.SlipNumber, group.TotalAmount)); addLine(" ");
                }
            }
            slipList.SetSource(displayLines);
        }

        private SlipGroup GetSelectedSlipGroup()
        {
            if (!int.TryParse(slipNoField.Text?.ToString().Trim() ?? "", out int targetSlipNo))
            {
                if (slipList.SelectedItem >= 0 && slipList.SelectedItem < lineToGroupMap.Count)
                {
                    var selectedGroup = lineToGroupMap[slipList.SelectedItem];
                    if (selectedGroup != null) targetSlipNo = selectedGroup.SlipNumber;
                }
            }
            if (targetSlipNo == -1) return null;
            return currentSlipGroups.FirstOrDefault(g => g.SlipNumber == targetSlipNo);
        }

        private void OnPaySlip()
        {
            var groupToPay = GetSelectedSlipGroup();
            if (groupToPay == null) { MessageBox.Query("Error", "Please select a valid Slip to pay.", "Ok"); return; }
            if (groupToPay.IsPaid) { MessageBox.Query("Info", $"Slip #{groupToPay.SlipNumber} is already CLEARED.", "Ok"); return; }
            ShowPaymentDialog(groupToPay);
        }

        private void OnEditStatus()
        {
            var groupToEdit = GetSelectedSlipGroup();
            if (groupToEdit == null) { MessageBox.Query("Error", "Please select a valid Slip to edit.", "Ok"); return; }
            ShowEditStatusDialog(groupToEdit);
        }

        private void ShowEditStatusDialog(SlipGroup group)
        {
            var dialog = new Dialog($"Edit Status: Slip #{group.SlipNumber}", 55, 12) { ColorScheme = Colors.DialogScheme };
            dialog.Add(new Label($"Total Bill Amount: ₹{group.TotalAmount:N2}") { X = 2, Y = 1 });
            dialog.Add(new Label($"Currently Paid:    ₹{group.TotalPaid:N2}") { X = 2, Y = 2 });
            dialog.Add(new Label("Set Paid Amount:") { X = 2, Y = 4 });
            var amountField = new TextField(group.TotalPaid.ToString("0.##")) { X = 20, Y = 4, Width = 15, ColorScheme = Colors.TextScheme };
            dialog.Add(amountField);

            var btnPending = new Button("Mark _Pending (₹0)") { X = Pos.Center() - 18, Y = 6, ColorScheme = Colors.ButtonScheme };
            btnPending.Clicked += () => {
                if (MessageBox.Query("Confirm", "Reset this slip to fully PENDING?", "Yes", "No") == 0)
                {
                    ProcessPaymentOverride(group.SlipNumber, 0); Application.RequestStop();
                }
            };

            var btnCleared = new Button("Mark _Cleared") { X = Pos.Center() + 4, Y = 6, ColorScheme = Colors.ButtonScheme };
            btnCleared.Clicked += () => {
                if (MessageBox.Query("Confirm", "Mark this slip as fully CLEARED?", "Yes", "No") == 0)
                {
                    ProcessPaymentOverride(group.SlipNumber, group.TotalAmount); Application.RequestStop();
                }
            };

            var btnSaveCustom = new Button("_Save Custom") { X = Pos.Center() - 15, Y = 8, ColorScheme = Colors.ButtonScheme, IsDefault = true };
            btnSaveCustom.Clicked += () => {
                if (decimal.TryParse(amountField.Text.ToString(), out decimal newAmt) && newAmt >= 0)
                {
                    if (newAmt > group.TotalAmount) { MessageBox.Query("Error", "Cannot pay more than the Total Bill.", "Ok"); return; }
                    ProcessPaymentOverride(group.SlipNumber, newAmt); Application.RequestStop();
                }
                else { MessageBox.Query("Error", "Invalid amount.", "Ok"); }
            };

            var btnCancel = new Button("Cance_l") { X = Pos.Center() + 4, Y = 8, ColorScheme = Colors.ErrorScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            dialog.Add(btnPending, btnCleared, btnSaveCustom, btnCancel);
            Application.Run(dialog);
        }

        private void ShowPaymentDialog(SlipGroup group)
        {
            var dialog = new Dialog($"Pay Slip #{group.SlipNumber}", 55, 12) { ColorScheme = Colors.DialogScheme };
            decimal remaining = group.Remaining;
            dialog.Add(new Label($"Total Amount:      ₹{group.TotalAmount:N2}") { X = 2, Y = 1 });
            dialog.Add(new Label($"Already Paid:      ₹{group.TotalPaid:N2}") { X = 2, Y = 2 });
            dialog.Add(new Label($"Remaining:         ₹{remaining:N2}") { X = 2, Y = 3 });
            dialog.Add(new Label("Enter Payment:") { X = 2, Y = 5 });
            var amountField = new TextField("") { X = 20, Y = 5, Width = 15, ColorScheme = Colors.TextScheme };
            dialog.Add(amountField);

            var btnPayPartial = new Button("Pay _Amount") { X = Pos.Center() - 15, Y = 7, ColorScheme = Colors.ButtonScheme, IsDefault = true };
            btnPayPartial.Clicked += () => {
                if (decimal.TryParse(amountField.Text.ToString(), out decimal payAmt) && payAmt > 0)
                {
                    if (payAmt > remaining) { MessageBox.Query("Error", "Amount exceeds balance.", "Ok"); return; }
                    ProcessPaymentOverride(group.SlipNumber, group.TotalPaid + payAmt); Application.RequestStop();
                }
                else { MessageBox.Query("Error", "Invalid Amount", "Ok"); }
            };

            var btnMarkCleared = new Button("Mark _Cleared") { X = Pos.Center() + 3, Y = 7, ColorScheme = Colors.ButtonScheme };
            btnMarkCleared.Clicked += () => {
                if (MessageBox.Query("Confirm", $"Mark Slip #{group.SlipNumber} as fully CLEARED?", "Yes", "No") == 0)
                {
                    ProcessPaymentOverride(group.SlipNumber, group.TotalAmount); Application.RequestStop();
                }
            };

            var btnCancel = new Button("Cance_l") { X = Pos.Center() - 5, Y = 9, ColorScheme = Colors.ErrorScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            dialog.Add(btnPayPartial, btnMarkCleared, btnCancel);
            Application.Run(dialog);
        }

        private void ProcessPaymentOverride(int slipNumber, decimal exactTotalPaid)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var items = db.PurchaseSlips.Where(s => s.SlipNumber == slipNumber).ToList();

                    // RECORD STATE FOR UNDO
                    var previousStates = items.Select(i => new { i.PurchaseSlipId, i.IsPaid, i.PaidAmount }).ToList();

                    foreach (var item in items) { item.IsPaid = false; item.PaidAmount = 0; }

                    decimal amountToDistribute = exactTotalPaid;
                    foreach (var item in items)
                    {
                        if (amountToDistribute <= 0) break;
                        if (amountToDistribute >= item.Amount)
                        {
                            item.PaidAmount = item.Amount; item.IsPaid = true; amountToDistribute -= item.Amount;
                        }
                        else
                        {
                            item.PaidAmount = amountToDistribute; amountToDistribute = 0;
                        }
                    }
                    db.SaveChanges();

                    // PUSH TO UNDO MANAGER
                    UndoManager.Push($"Payment/Status Change on Slip #{slipNumber}", () => {
                        using (var dbUndo = new AppDbContext())
                        {
                            foreach (var state in previousStates)
                            {
                                var item = dbUndo.PurchaseSlips.Find(state.PurchaseSlipId);
                                if (item != null) { item.IsPaid = state.IsPaid; item.PaidAmount = state.PaidAmount; }
                            }
                            dbUndo.SaveChanges();
                        }
                    }, () => OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text)));
                }

                OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
                slipNoField.Text = "";
                MessageBox.Query("Success", "Slip status successfully updated.", "Ok");
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnMasterClear()
        {
            var pendingGroups = currentSlipGroups.Where(g => !g.IsPaid).ToList();
            if (pendingGroups.Count == 0) { MessageBox.Query("Info", "There are no pending slips to clear.", "Ok"); return; }
            if (MessageBox.Query("Master Clear", $"Clear {pendingGroups.Count} Pending slip(s)?", "Yes", "No") != 0) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var partyId = pendingGroups.First().Items.First().PartyId;
                    var itemsToClear = db.PurchaseSlips.Where(s => s.PartyId == partyId && !s.IsPaid).ToList();

                    // RECORD STATE FOR UNDO
                    var previousStates = itemsToClear.Select(i => new { i.PurchaseSlipId, i.IsPaid, i.PaidAmount }).ToList();

                    foreach (var item in itemsToClear) { item.IsPaid = true; item.PaidAmount = item.Amount; }
                    db.SaveChanges();

                    // PUSH TO UNDO MANAGER
                    UndoManager.Push($"Master Clear of {pendingGroups.Count} Slips", () => {
                        using (var dbUndo = new AppDbContext())
                        {
                            foreach (var state in previousStates)
                            {
                                var item = dbUndo.PurchaseSlips.Find(state.PurchaseSlipId);
                                if (item != null) { item.IsPaid = state.IsPaid; item.PaidAmount = state.PaidAmount; }
                            }
                            dbUndo.SaveChanges();
                        }
                    }, () => OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text)));
                }

                OnPartySelected(new ListViewItemEventArgs(partyCombo.SelectedItem, partyCombo.Text));
                slipNoField.Text = "";
                MessageBox.Query("Success", "All pending slips have been cleared.", "Ok");
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}