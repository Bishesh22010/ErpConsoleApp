using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpConsoleApp.UI
{
    public class PurchaseWindow : Window
    {
        private TextField dateField;
        private ComboBox partyIdCombo;
        private TextView partyDetailsView;
        private ListView availableItemsList;
        private ComboBox itemCodeCombo;
        private TextField quantityField;
        private TextField amountField;

        private ListView cartListView;
        private Label totalAmountLabel;

        private List<Party> allParties = new List<Party>();
        private List<Item> allItems = new List<Item>();
        private Party selectedParty = null;
        private List<CartItem> cart = new List<CartItem>();

        private int? editingSlipNumber = null;

        private class CartItem
        {
            public int SrNo { get; set; }
            public Item Item { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }

            public override string ToString()
            {
                string shortName = Item.ItemName.Length > 15 ? Item.ItemName.Substring(0, 12) + "..." : Item.ItemName;
                string code = (Item.ItemCode ?? "").Length > 6 ? Item.ItemCode.Substring(0, 6) : (Item.ItemCode ?? "");
                return string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4:0.##} | {4,6:0.##} | {5,8:0.##}",
                    SrNo, code, shortName, Quantity, UnitPrice, Amount);
            }
        }

        public PurchaseWindow(int? slipNoToEdit = null) : base(slipNoToEdit.HasValue ? $"Edit Purchase Slip #{slipNoToEdit} (Press ESC to go back)" : "New Purchase Slip (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            LoadDataFromDb();
            SetupUI();

            if (slipNoToEdit.HasValue) LoadSlipData(slipNoToEdit.Value);
        }

        private void SetupUI()
        {
            var leftPane = new FrameView("Party & Item Selection") { X = 0, Y = 0, Width = Dim.Percent(50), Height = Dim.Fill() };

            leftPane.Add(new Label("Date (DD-MM-YYYY):") { X = 1, Y = 1 });
            dateField = new TextField(DateTime.Now.ToString("dd-MM-yyyy")) { X = 20, Y = 1, Width = 15, ColorScheme = Colors.TextScheme };
            leftPane.Add(dateField);

            leftPane.Add(new Label("Party ID:") { X = 1, Y = 3 });
            partyIdCombo = new ComboBox() { X = 15, Y = 3, Width = 20, Height = 5, ColorScheme = Colors.TextScheme };
            partyIdCombo.SetSource(allParties.Select(p => p.PartyCode).ToList());
            partyIdCombo.SelectedItemChanged += OnPartySelected;
            leftPane.Add(partyIdCombo);

            partyDetailsView = new TextView() { X = 1, Y = 5, Width = Dim.Fill(1), Height = 4, ReadOnly = true, ColorScheme = Colors.ResultScheme, Text = "Select a Party ID to view details..." };
            leftPane.Add(partyDetailsView);

            leftPane.Add(new Label("--- Available Items ---") { X = 1, Y = 10 });
            availableItemsList = new ListView() { X = 1, Y = 11, Width = Dim.Fill(1), Height = Dim.Fill(11), ColorScheme = Colors.TextScheme, CanFocus = false };
            availableItemsList.SetSource(allItems.Select(i => $"[{i.ItemCode}] {i.ItemName}").ToList());
            leftPane.Add(availableItemsList);

            leftPane.Add(new Label("Item Code:") { X = 1, Y = Pos.AnchorEnd(9) });
            itemCodeCombo = new ComboBox() { X = 15, Y = Pos.AnchorEnd(9), Width = 20, Height = 4, ColorScheme = Colors.TextScheme };
            itemCodeCombo.SetSource(allItems.Select(i => i.ItemCode).ToList());
            leftPane.Add(itemCodeCombo);

            leftPane.Add(new Label("Quantity:") { X = 1, Y = Pos.AnchorEnd(7) });
            quantityField = new TextField("") { X = 15, Y = Pos.AnchorEnd(7), Width = 20, ColorScheme = Colors.TextScheme };
            leftPane.Add(quantityField);

            leftPane.Add(new Label("Price/Unit:") { X = 1, Y = Pos.AnchorEnd(5) });
            amountField = new TextField("") { X = 15, Y = Pos.AnchorEnd(5), Width = 20, ColorScheme = Colors.TextScheme };
            leftPane.Add(amountField);

            var btnAdd = new Button("_Add to Slip") { X = Pos.Center(), Y = Pos.AnchorEnd(3), ColorScheme = Colors.ButtonScheme };
            btnAdd.Clicked += OnAddToCart;
            leftPane.Add(btnAdd);

            var rightPane = new FrameView("Current Slip Items") { X = Pos.Right(leftPane), Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

            var header = new Label(string.Format("{0,-5} | {1,-6} | {2,-15} | {3,4} | {4,6} | {5,8}", "SR NO", "CODE", "NAME", "QTY", "PRICE", "TOTAL")) { X = 1, Y = 0, ColorScheme = Colors.MenuScheme };
            rightPane.Add(header);

            cartListView = new ListView() { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(6), ColorScheme = Colors.TextScheme };
            cartListView.SetSource(new List<string>());
            rightPane.Add(cartListView);

            totalAmountLabel = new Label("Total Amount: ₹0.00") { X = 1, Y = Pos.AnchorEnd(5), ColorScheme = Colors.ResultScheme };

            var btnEditItem = new Button("_Edit") { X = Pos.Right(totalAmountLabel) + 3, Y = Pos.AnchorEnd(5), ColorScheme = Colors.ButtonScheme };
            btnEditItem.Clicked += OnEditCartItem;

            var btnRemoveItem = new Button("_Remove") { X = Pos.Right(btnEditItem) + 2, Y = Pos.AnchorEnd(5), ColorScheme = Colors.ErrorScheme };
            btnRemoveItem.Clicked += OnRemoveCartItem;

            rightPane.Add(totalAmountLabel, btnEditItem, btnRemoveItem);

            var btnSave = new Button("_Save Slip") { X = 1, Y = Pos.AnchorEnd(3), IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnSave.Clicked += OnSaveSlip;

            var btnLoad = new Button("_Load Slip") { X = Pos.Right(btnSave) + 1, Y = Pos.AnchorEnd(3), ColorScheme = Colors.ButtonScheme };
            btnLoad.Clicked += OnLoadSlipDialog;

            // --- NEW UNDO BUTTON ---
            var btnUndo = new Button("U_ndo (Ctrl+Z)") { X = Pos.Right(btnLoad) + 1, Y = Pos.AnchorEnd(3), ColorScheme = Colors.ButtonScheme };
            btnUndo.Clicked += UndoManager.Undo;

            var btnViewSlips = new Button("_View Slips") { X = Pos.Right(btnUndo) + 1, Y = Pos.AnchorEnd(3), ColorScheme = Colors.ButtonScheme };
            btnViewSlips.Clicked += () => {
                if (selectedParty == null) { MessageBox.Query("Error", "Please select a Party ID on the left first to view their slips.", "Ok"); return; }
                Program.OpenModal(new ViewPartySlipsWindow(selectedParty));
            };

            var btnCancel = new Button("_Cancel") { X = Pos.Right(btnViewSlips) + 1, Y = Pos.AnchorEnd(3), ColorScheme = Colors.ErrorScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save | [Alt+L] Load | [Alt+A] Add | [Alt+E] Edit | [Ctrl+Z] Undo")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                ColorScheme = Colors.ResultScheme
            };

            rightPane.Add(btnSave, btnLoad, btnUndo, btnViewSlips, btnCancel, shortcutsLabel);

            Add(leftPane, rightPane);
            partyIdCombo.SetFocus();
        }

        private void LoadDataFromDb()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    allParties = db.Parties.OrderBy(p => p.PartyCode).ToList();
                    allItems = db.Items.OrderBy(i => i.ItemCode).ToList();
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.InnerException?.Message ?? e.Message); }
        }

        private void OnLoadSlipDialog()
        {
            var dialog = new Dialog("Load Existing Slip", 45, 10) { ColorScheme = Colors.DialogScheme };
            dialog.Add(new Label("Enter Slip No to Edit:") { X = 2, Y = 2 });
            var inputField = new TextField("") { X = 25, Y = 2, Width = 15, ColorScheme = Colors.TextScheme };
            var btnOk = new Button("_Load") { X = 10, Y = 5, IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnOk.Clicked += () => {
                if (int.TryParse(inputField.Text.ToString(), out int slipNo)) { Application.RequestStop(); LoadSlipData(slipNo); }
                else { MessageBox.Query("Error", "Invalid slip number.", "Ok"); }
            };
            var btnCancel = new Button("_Cancel") { X = 25, Y = 5, ColorScheme = Colors.ButtonScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            dialog.Add(inputField, btnOk, btnCancel);
            inputField.SetFocus();
            Application.Run(dialog);
        }

        private void LoadSlipData(int slipNo)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var slips = db.PurchaseSlips.Include(s => s.Party).Where(s => s.SlipNumber == slipNo).ToList();
                    if (slips.Count == 0) { MessageBox.Query("Error", $"Slip #{slipNo} not found.", "Ok"); return; }

                    editingSlipNumber = slipNo;
                    Title = $"Edit Purchase Slip #{slipNo} (Press ESC to go back)";
                    var first = slips.First();
                    dateField.Text = first.SlipDate.ToString("dd-MM-yyyy");

                    selectedParty = allParties.FirstOrDefault(p => p.PartyCode == first.Party.PartyCode);
                    if (selectedParty != null)
                    {
                        partyIdCombo.Text = selectedParty.PartyCode;
                        partyDetailsView.Text = $"Name:  {selectedParty.Name}\nPhone: {selectedParty.PhoneNumber ?? "N/A"}\nGST:   {selectedParty.GstNumber ?? "N/A"}";
                    }

                    cart.Clear();
                    int srNo = 1;
                    foreach (var s in slips)
                    {
                        var item = allItems.FirstOrDefault(i => i.ItemCode == s.ItemCode) ?? new Item { ItemCode = s.ItemCode, ItemName = s.ItemName };
                        cart.Add(new CartItem { SrNo = srNo++, Item = item, Quantity = s.Quantity, UnitPrice = s.UnitPrice, Amount = s.Amount });
                    }
                    RefreshCartView();
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.InnerException?.Message ?? e.Message); }
        }

        private void OnPartySelected(ListViewItemEventArgs args)
        {
            string selectedCode = args.Value?.ToString() ?? "";
            selectedParty = allParties.FirstOrDefault(p => p.PartyCode == selectedCode);
            if (selectedParty != null) { partyDetailsView.Text = $"Name:  {selectedParty.Name}\nPhone: {selectedParty.PhoneNumber ?? "N/A"}\nGST:   {selectedParty.GstNumber ?? "N/A"}"; }
            else { partyDetailsView.Text = "Party not found."; }
        }

        private void OnAddToCart()
        {
            string code = itemCodeCombo.Text?.ToString().Trim() ?? "";
            string qtyStr = quantityField.Text?.ToString().Trim() ?? "";
            string amtStr = amountField.Text?.ToString().Trim() ?? "";

            if (selectedParty == null) { MessageBox.Query("Validation", "Please select a valid Party ID first.", "Ok"); return; }
            Item item = allItems.FirstOrDefault(i => i.ItemCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (item == null) { MessageBox.Query("Validation", $"Item with Code '{code}' not found.", "Ok"); return; }
            if (!decimal.TryParse(qtyStr, out decimal quantity) || quantity <= 0) { MessageBox.Query("Validation", "Please enter a valid quantity.", "Ok"); return; }
            if (!decimal.TryParse(amtStr, out decimal unitPrice) || unitPrice <= 0) { MessageBox.Query("Validation", "Please enter a valid price/unit.", "Ok"); return; }

            decimal totalAmount = quantity * unitPrice;
            cart.Add(new CartItem { SrNo = cart.Count + 1, Item = item, Quantity = quantity, UnitPrice = unitPrice, Amount = totalAmount });
            RefreshCartView();
            itemCodeCombo.Text = ""; quantityField.Text = ""; amountField.Text = ""; itemCodeCombo.SetFocus();
        }

        private void OnEditCartItem()
        {
            if (cartListView.SelectedItem >= 0 && cartListView.SelectedItem < cart.Count)
            {
                var item = cart[cartListView.SelectedItem];
                itemCodeCombo.Text = item.Item.ItemCode; quantityField.Text = item.Quantity.ToString(); amountField.Text = item.UnitPrice.ToString();
                cart.RemoveAt(cartListView.SelectedItem); ReassignSerialNumbers(); RefreshCartView(); quantityField.SetFocus();
            }
            else { MessageBox.Query("Error", "Please select an item from the cart to edit.", "Ok"); }
        }

        private void OnRemoveCartItem()
        {
            if (cartListView.SelectedItem >= 0 && cartListView.SelectedItem < cart.Count)
            {
                cart.RemoveAt(cartListView.SelectedItem); ReassignSerialNumbers(); RefreshCartView();
            }
            else { MessageBox.Query("Error", "Please select an item from the cart to remove.", "Ok"); }
        }

        private void ReassignSerialNumbers() { for (int i = 0; i < cart.Count; i++) { cart[i].SrNo = i + 1; } }

        private void RefreshCartView()
        {
            if (cart.Count == 0) { cartListView.SetSource(new List<string> { "Slip is empty." }); totalAmountLabel.Text = "Total Amount: ₹0.00"; }
            else { cartListView.SetSource(cart.Select(c => c.ToString()).ToList()); totalAmountLabel.Text = $"Total Amount: ₹{cart.Sum(c => c.Amount):N2}"; }
        }

        private void OnSaveSlip()
        {
            if (cart.Count == 0) { MessageBox.Query("Error", "No items added to the slip.", "Ok"); return; }
            if (!DateTime.TryParseExact(dateField.Text.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime slipDate))
            {
                MessageBox.Query("Validation", "Invalid date format. Please use DD-MM-YYYY.", "Ok"); return;
            }

            string actionType = editingSlipNumber.HasValue ? "Update" : "Generate";
            if (MessageBox.Query($"Confirm {actionType}", $"{actionType} slip for {selectedParty.Name} totaling ₹{cart.Sum(c => c.Amount):N2}?", "Yes", "No") != 0) { return; }

            try
            {
                int finalSlipNumber;
                List<PurchaseSlip> backupOldSlips = new List<PurchaseSlip>();

                using (var db = new AppDbContext())
                {
                    decimal previousPaidAmount = 0;

                    if (editingSlipNumber.HasValue)
                    {
                        finalSlipNumber = editingSlipNumber.Value;
                        var existingItems = db.PurchaseSlips.Where(s => s.SlipNumber == finalSlipNumber).ToList();
                        previousPaidAmount = existingItems.Sum(s => s.PaidAmount);

                        if (cart.Sum(c => c.Amount) < previousPaidAmount)
                        {
                            MessageBox.Query("Payment Error", $"Cannot reduce slip total below the already paid amount (₹{previousPaidAmount:N2}).", "Ok"); return;
                        }

                        // Backup for Undo Manager
                        backupOldSlips = existingItems.Select(s => new PurchaseSlip
                        {
                            SlipNumber = s.SlipNumber,
                            SlipDate = s.SlipDate,
                            ItemCode = s.ItemCode,
                            ItemName = s.ItemName,
                            Quantity = s.Quantity,
                            QtyType = s.QtyType ?? "",
                            UnitPrice = s.UnitPrice,
                            Amount = s.Amount,
                            PartyId = s.PartyId,
                            IsPaid = s.IsPaid,
                            PaidAmount = s.PaidAmount
                        }).ToList();

                        db.PurchaseSlips.RemoveRange(existingItems);
                    }
                    else
                    {
                        finalSlipNumber = 1;
                        if (db.PurchaseSlips.Any()) { finalSlipNumber = db.PurchaseSlips.Max(s => s.SlipNumber) + 1; }
                    }

                    var newSlipsToInsert = new List<PurchaseSlip>();
                    foreach (var cartItem in cart)
                    {
                        var newSlip = new PurchaseSlip
                        {
                            SlipNumber = finalSlipNumber,
                            SlipDate = slipDate,
                            ItemCode = cartItem.Item.ItemCode,
                            ItemName = cartItem.Item.ItemName,
                            Quantity = cartItem.Quantity,
                            QtyType = "",
                            UnitPrice = cartItem.UnitPrice,
                            Amount = cartItem.Amount,
                            PartyId = selectedParty.PartyId,
                            IsPaid = false,
                            PaidAmount = 0
                        };
                        newSlipsToInsert.Add(newSlip);
                        db.PurchaseSlips.Add(newSlip);
                    }

                    decimal amountToDistribute = previousPaidAmount;
                    foreach (var slip in newSlipsToInsert)
                    {
                        if (amountToDistribute <= 0) break;
                        if (amountToDistribute >= slip.Amount) { slip.PaidAmount = slip.Amount; slip.IsPaid = true; amountToDistribute -= slip.Amount; }
                        else { slip.PaidAmount = amountToDistribute; amountToDistribute = 0; }
                    }
                    db.SaveChanges();
                }

                // --- PUSH TO UNDO MANAGER ---
                if (editingSlipNumber.HasValue)
                {
                    UndoManager.Push($"Edit of Slip #{finalSlipNumber}", () => {
                        using (var dbUndo = new AppDbContext())
                        {
                            var currentSlips = dbUndo.PurchaseSlips.Where(s => s.SlipNumber == finalSlipNumber);
                            dbUndo.PurchaseSlips.RemoveRange(currentSlips);
                            dbUndo.PurchaseSlips.AddRange(backupOldSlips);
                            dbUndo.SaveChanges();
                        }
                    });
                }
                else
                {
                    UndoManager.Push($"Creation of Slip #{finalSlipNumber}", () => {
                        using (var dbUndo = new AppDbContext())
                        {
                            var slipsToRemove = dbUndo.PurchaseSlips.Where(s => s.SlipNumber == finalSlipNumber);
                            dbUndo.PurchaseSlips.RemoveRange(slipsToRemove);
                            dbUndo.SaveChanges();
                        }
                    });
                }

                MessageBox.Query("Success", $"Purchase Slip {actionType}d successfully!", "Ok");

                cart.Clear(); RefreshCartView(); itemCodeCombo.Text = ""; quantityField.Text = ""; amountField.Text = "";
                editingSlipNumber = null; Title = "New Purchase Slip (Press ESC to go back)"; partyIdCombo.SetFocus();

            }
            catch (Exception e) { Program.ShowError("Database Error", e.InnerException?.Message ?? e.Message); }
        }
    }
}