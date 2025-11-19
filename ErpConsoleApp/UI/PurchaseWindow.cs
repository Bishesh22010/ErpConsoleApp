using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// Window for creating a new Purchase Slip.
    /// Updated to be Full Screen with Center Alignment.
    /// </summary>
    public class PurchaseWindow : Window
    {
        private TextField dateField;
        private ComboBox partyCombo;
        private TextField itemField;
        private TextField amountField;

        private List<string> partyNames = new List<string>();

        public PurchaseWindow() : base("New Purchase Slip (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;

            // --- FULL SCREEN SETTINGS ---
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();
            Modal = true;

            // --- ESCAPE KEY HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc)
                {
                    Application.RequestStop();
                    e.Handled = true;
                }
            };

            LoadPartiesFromDb();

            // --- CENTERED CONTAINER ---
            // We create a view in the center to hold our form controls
            var container = new View()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = 60,
                Height = 20
            };

            int y = 0;

            // --- Date ---
            container.Add(new Label("Date:") { X = 0, Y = y });
            dateField = new TextField(DateTime.Now.ToString("yyyy-MM-dd"))
            {
                X = 15,
                Y = y,
                Width = 20,
                ColorScheme = Colors.TextScheme
            };
            container.Add(dateField);

            y += 2;

            // --- Party Name (ComboBox) ---
            container.Add(new Label("Party Name:") { X = 0, Y = y });
            partyCombo = new ComboBox()
            {
                X = 15,
                Y = y,
                Width = 35,
                Height = 5, // Needs height for dropdown
                ColorScheme = Colors.TextScheme
            };
            partyCombo.SetSource(partyNames);
            container.Add(partyCombo);

            y += 2;

            // --- Item Name ---
            container.Add(new Label("Item Name:") { X = 0, Y = y });
            itemField = new TextField("")
            {
                X = 15,
                Y = y,
                Width = 35,
                ColorScheme = Colors.TextScheme
            };
            container.Add(itemField);

            y += 2;

            // --- Amount ---
            container.Add(new Label("Amount:") { X = 0, Y = y });
            amountField = new TextField("")
            {
                X = 15,
                Y = y,
                Width = 20,
                ColorScheme = Colors.TextScheme
            };
            container.Add(amountField);

            y += 4; // Add extra space before buttons

            // --- Buttons ---
            var generateButton = new Button("_Generate Slip")
            {
                X = 5,
                Y = y,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            generateButton.Clicked += OnGenerateSlip;

            var cancelButton = new Button("_Cancel")
            {
                X = 25,
                Y = y,
                ColorScheme = Colors.ButtonScheme
            };
            cancelButton.Clicked += () => Application.RequestStop();

            container.Add(generateButton, cancelButton);

            // Add the centered container to the main window
            Add(container);

            // Set initial focus
            dateField.SetFocus();
        }

        private void LoadPartiesFromDb()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    partyNames = db.Parties.Select(p => p.Name).ToList();
                }
                catch (Exception e)
                {
                    Program.ShowError("DB Error", $"Could not load parties:\n{e.Message}");
                    partyNames = new List<string>();
                }
            }
        }

        private void OnGenerateSlip()
        {
            // --- 1. Validation ---
            string partyName = partyCombo.Text?.ToString() ?? "";
            string itemName = itemField.Text?.ToString() ?? "";
            string amountText = amountField.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(partyName) ||
                string.IsNullOrWhiteSpace(itemName) ||
                !decimal.TryParse(amountText, out decimal amount) ||
                amount <= 0)
            {
                Program.ShowError("Validation Error", "Party, Item, and a valid Amount are required.");
                return;
            }

            if (!DateTime.TryParse(dateField.Text.ToString(), out DateTime slipDate))
            {
                Program.ShowError("Validation Error", "Please enter a valid date (YYYY-MM-DD).");
                return;
            }

            // --- 2. Database Logic ---
            try
            {
                using (var db = new AppDbContext())
                {
                    // Check if party exists
                    Party party = db.Parties.FirstOrDefault(p => p.Name == partyName);

                    if (party == null)
                    {
                        Program.ShowError("Party Not Found",
                            $"Party '{partyName}' does not exist.\n\n" +
                            "Please add it first using the 'Add & Delete Party' option.");
                        return;
                    }

                    // Create the purchase slip
                    var slip = new PurchaseSlip
                    {
                        SlipDate = slipDate,
                        ItemName = itemName,
                        Amount = amount,
                        PartyId = party.PartyId,
                        IsPaid = false // Default to not paid
                    };

                    db.PurchaseSlips.Add(slip);
                    db.SaveChanges();
                }

                // --- 3. Success ---
                Program.ShowMessage("Success", $"Purchase Slip generated for {partyName}\nAmount: ₹{amount:N2}\n\nPress Enter to continue...");

                // Clear form for next entry
                itemField.Text = "";
                amountField.Text = "";
                itemField.SetFocus();
            }
            catch (Exception e)
            {
                string error = $"Could not save slip:\n{e.Message}\n\n" +
                               "Tip: Is 'erp.db' locked by another program (like DB Browser)?";
                Program.ShowError("Database Error", error);
            }
        }
    }
}