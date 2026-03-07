using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class PurchaseWindow : Window
    {
        // UI Elements - Left Pane
        private TextField dateField;
        private ComboBox partyIdCombo;
        private TextView partyDetailsView;
        private ListView availableItemsList;
        private ComboBox itemCodeCombo; // Changed to ComboBox for autocomplete search
        private TextField amountField;

        // UI Elements - Right Pane
        private ListView cartListView;
        private Label totalAmountLabel;

        // Data State
        private List<Party> allParties = new List<Party>();
        private List<Item> allItems = new List<Item>();
        private Party selectedParty = null;
        private List<CartItem> cart = new List<CartItem>();

        // Helper class for the right-side cart
        private class CartItem
        {
            public Item Item { get; set; }
            public decimal Amount { get; set; }
            public override string ToString() => $"[{Item.ItemCode}] {Item.ItemName,-15} | ₹{Amount,10:N2}";
        }

        public PurchaseWindow() : base("New Purchase Slip (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            LoadDataFromDb();
            SetupUI();
        }

        private void SetupUI()
        {
            // --- LEFT PANE: Data Entry ---
            var leftPane = new FrameView("Party & Item Selection")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill()
            };

            leftPane.Add(new Label("Date:") { X = 1, Y = 1 });
            dateField = new TextField(DateTime.Now.ToString("yyyy-MM-dd")) { X = 15, Y = 1, Width = 20, ColorScheme = Colors.TextScheme };
            leftPane.Add(dateField);

            leftPane.Add(new Label("Party ID:") { X = 1, Y = 3 });
            partyIdCombo = new ComboBox() { X = 15, Y = 3, Width = 20, Height = 5, ColorScheme = Colors.TextScheme };
            partyIdCombo.SetSource(allParties.Select(p => p.PartyCode).ToList());
            partyIdCombo.SelectedItemChanged += OnPartySelected;
            leftPane.Add(partyIdCombo);

            partyDetailsView = new TextView()
            {
                X = 1,
                Y = 5,
                Width = Dim.Fill(1),
                Height = 4,
                ReadOnly = true,
                ColorScheme = Colors.ResultScheme,
                Text = "Select a Party ID to view details..."
            };
            leftPane.Add(partyDetailsView);

            leftPane.Add(new Label("--- Available Items ---") { X = 1, Y = 10 });
            availableItemsList = new ListView()
            {
                X = 1,
                Y = 11,
                Width = Dim.Fill(1),
                Height = 6,
                ColorScheme = Colors.TextScheme,
                CanFocus = false
            };
            availableItemsList.SetSource(allItems.Select(i => $"[{i.ItemCode}] {i.ItemName}").ToList());
            leftPane.Add(availableItemsList);

            leftPane.Add(new Label("Item Code:") { X = 1, Y = 18 });
            itemCodeCombo = new ComboBox() { X = 15, Y = 18, Width = 20, Height = 4, ColorScheme = Colors.TextScheme };
            itemCodeCombo.SetSource(allItems.Select(i => i.ItemCode).ToList()); // Load Item Codes
            leftPane.Add(itemCodeCombo);

            leftPane.Add(new Label("Amount:") { X = 1, Y = 20 });
            amountField = new TextField("") { X = 15, Y = 20, Width = 20, ColorScheme = Colors.TextScheme };
            leftPane.Add(amountField);

            var btnAdd = new Button("_Add to Slip") { X = Pos.Center(), Y = 22, ColorScheme = Colors.ButtonScheme };
            btnAdd.Clicked += OnAddToCart;
            leftPane.Add(btnAdd);


            // --- RIGHT PANE: Cart & Generation ---
            var rightPane = new FrameView("Current Slip Items")
            {
                X = Pos.Right(leftPane),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            cartListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(4),
                ColorScheme = Colors.TextScheme
            };
            rightPane.Add(cartListView);

            totalAmountLabel = new Label("Total Amount: ₹0.00")
            {
                X = 1,
                Y = Pos.AnchorEnd(4), // Moved up to make room
                ColorScheme = Colors.ResultScheme
            };
            rightPane.Add(totalAmountLabel);

            var btnGenerate = new Button("_Generate Slip") { X = 1, Y = Pos.AnchorEnd(2), IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnGenerate.Clicked += OnGenerateSlip;

            var btnCancel = new Button("_Cancel") { X = Pos.Right(btnGenerate) + 2, Y = Pos.AnchorEnd(2), ColorScheme = Colors.ErrorScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            // --- NEW: App-wide Shortcut Display Pattern ---
            var shortcutsLabel = new Label("Shortcuts: [Alt+A] Add | [Alt+G] Generate | [Alt+C]/[ESC] Cancel | [Tab] Navigate")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1), // Placed at the very bottom
                ColorScheme = Colors.ResultScheme
            };

            rightPane.Add(btnGenerate, btnCancel, shortcutsLabel);

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
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnPartySelected(ListViewItemEventArgs args)
        {
            string selectedCode = args.Value?.ToString() ?? "";
            selectedParty = allParties.FirstOrDefault(p => p.PartyCode == selectedCode);

            if (selectedParty != null)
            {
                partyDetailsView.Text = $"Name:  {selectedParty.Name}\nPhone: {selectedParty.PhoneNumber ?? "N/A"}\nGST:   {selectedParty.GstNumber ?? "N/A"}";
            }
            else
            {
                partyDetailsView.Text = "Party not found.";
            }
        }

        private void OnAddToCart()
        {
            string code = itemCodeCombo.Text?.ToString().Trim() ?? "";
            string amtStr = amountField.Text?.ToString().Trim() ?? "";

            if (selectedParty == null)
            {
                Program.ShowError("Validation", "Please select a valid Party ID first."); return;
            }

            Item item = allItems.FirstOrDefault(i => i.ItemCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                Program.ShowError("Validation", $"Item with Code '{code}' not found."); return;
            }

            if (!decimal.TryParse(amtStr, out decimal amount) || amount <= 0)
            {
                Program.ShowError("Validation", "Please enter a valid amount."); return;
            }

            // Add to Cart
            cart.Add(new CartItem { Item = item, Amount = amount });
            RefreshCartView();

            // Clear inputs for next item
            itemCodeCombo.Text = "";
            amountField.Text = "";
            itemCodeCombo.SetFocus();
        }

        private void RefreshCartView()
        {
            if (cart.Count == 0)
            {
                cartListView.SetSource(new List<string> { "Slip is empty." });
                totalAmountLabel.Text = "Total Amount: ₹0.00";
            }
            else
            {
                cartListView.SetSource(cart.Select(c => c.ToString()).ToList());
                decimal total = cart.Sum(c => c.Amount);
                totalAmountLabel.Text = $"Total Amount: ₹{total:N2}";
            }
        }

        private void OnGenerateSlip()
        {
            if (cart.Count == 0)
            {
                Program.ShowError("Error", "No items added to the slip."); return;
            }

            if (!DateTime.TryParse(dateField.Text.ToString(), out DateTime slipDate))
            {
                Program.ShowError("Validation", "Invalid date format."); return;
            }

            if (!Program.ShowQuery("Confirm Generation", $"Generate slip for {selectedParty.Name} totaling ₹{cart.Sum(c => c.Amount):N2}?"))
            {
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    foreach (var cartItem in cart)
                    {
                        db.PurchaseSlips.Add(new PurchaseSlip
                        {
                            SlipDate = slipDate,
                            ItemName = cartItem.Item.ItemName,
                            Amount = cartItem.Amount,
                            PartyId = selectedParty.PartyId,
                            IsPaid = false,
                            PaidAmount = 0
                        });
                    }
                    db.SaveChanges();
                }

                Program.ShowMessage("Success", "Purchase Slip generated successfully!");

                cart.Clear();
                RefreshCartView();
                itemCodeCombo.Text = "";
                amountField.Text = "";
                partyIdCombo.SetFocus();

            }
            catch (Exception e) { Program.ShowError("Database Error", e.Message); }
        }
    }
}