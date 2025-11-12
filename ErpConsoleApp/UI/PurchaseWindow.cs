using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// Modal window for creating a new Purchase Slip.
    /// </summary>
    public class PurchaseWindow : Window
    {
        private TextField dateField;
        private ComboBox partyCombo;
        private TextField itemField;
        private TextField amountField;

        private List<string> partyNames = new List<string>();

        public PurchaseWindow() : base("New Purchase Slip")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center();
            Y = Pos.Center() - 4;
            Width = 60;
            Height = 16;
            Modal = true;

            LoadPartiesFromDb();

            int y = 2; // Vertical position tracker

            // --- Date ---
            Add(new Label("Date:") { X = 2, Y = y });
            dateField = new TextField(DateTime.Now.ToString("yyyy-MM-dd"))
            {
                X = 15,
                Y = y,
                Width = 15,
                ColorScheme = Colors.TextScheme
            };
            Add(dateField);

            y += 2;

            // --- Party Name (ComboBox) ---
            Add(new Label("Party Name:") { X = 2, Y = y });
            partyCombo = new ComboBox()
            {
                X = 15,
                Y = y,
                Width = 35,
                ColorScheme = Colors.TextScheme
            };
            partyCombo.SetSource(partyNames);
            Add(partyCombo);

            y += 2;

            // --- Item Name ---
            Add(new Label("Item Name:") { X = 2, Y = y });
            itemField = new TextField("")
            {
                X = 15,
                Y = y,
                Width = 35,
                ColorScheme = Colors.TextScheme
            };
            Add(itemField);

            y += 2;

            // --- Amount ---
            Add(new Label("Amount:") { X = 2, Y = y });
            amountField = new TextField("")
            {
                X = 15,
                Y = y,
                Width = 15,
                ColorScheme = Colors.TextScheme
            };
            Add(amountField);

            y += 4; // Add extra space

            // --- Buttons ---
            var generateButton = new Button("_Generate Slip")
            {
                X = Pos.Center() - 15,
                Y = y,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            generateButton.Clicked += OnGenerateSlip;

            var cancelButton = new Button("_Cancel")
            {
                X = Pos.Center() + 5,
                Y = y,
                ColorScheme = Colors.ButtonScheme
            };
            cancelButton.Clicked += () => Application.RequestStop(); // Close this modal window

            Add(generateButton, cancelButton);

            // Set initial focus
            dateField.SetFocus();
        }

        /// <summary>
        /// Loads all existing party names from the database into the ComboBox list.
        /// </summary>
        private void LoadPartiesFromDb()
        {
            // Use a fresh DbContext for this operation
            using (var db = new AppDbContext())
            {
                try
                {
                    partyNames = db.Parties.Select(p => p.Name).ToList();
                }
                catch (Exception e)
                {
                    // Show error but continue, list will just be empty
                    Program.ShowError("DB Error", $"Could not load parties:\n{e.Message}");
                    partyNames = new List<string>(); // Ensure list is not null
                }
            }

            // Add a default entry for testing if the list is empty
            if (partyNames.Count == 0)
            {
                partyNames.Add("XYZ Party");
            }
        }

        /// <summary>
        /// Validates the form and saves the new Purchase Slip to the database.
        /// </summary>
        private void OnGenerateSlip()
        {
            // --- 1. Validation ---
            string partyName = partyCombo.Text.ToString();
            string itemName = itemField.Text.ToString();

            if (string.IsNullOrWhiteSpace(partyName) ||
                string.IsNullOrWhiteSpace(itemName) ||
                !decimal.TryParse(amountField.Text.ToString(), out decimal amount) ||
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
                // Use a fresh DbContext for this "transaction"
                using (var db = new AppDbContext())
                {
                    // Find or create the party
                    Party party = db.Parties.FirstOrDefault(p => p.Name == partyName);
                    if (party == null)
                    {
                        // Party doesn't exist, create it
                        party = new Party { Name = partyName };
                        db.Parties.Add(party);
                        // We must save here so the 'party' object gets an ID
                        db.SaveChanges();
                    }

                    // Create the purchase slip
                    var slip = new PurchaseSlip
                    {
                        SlipDate = slipDate,
                        ItemName = itemName,
                        Amount = amount,
                        PartyId = party.PartyId // Link to the party
                    };

                    db.PurchaseSlips.Add(slip);

                    // Save all changes (slip and new party if any)
                    db.SaveChanges();
                }

                // --- 3. Success ---
                Program.ShowMessage("Success", $"Purchase Slip generated for {partyName}\nAmount: {amount:C}");

                // Update the ComboBox list in case we added a new party
                if (!partyNames.Contains(partyName))
                {
                    partyNames.Add(partyName);
                    partyCombo.SetSource(partyNames);
                }

                // Clear form for next entry (but keep date and party)
                itemField.Text = "";
                amountField.Text = "";
                itemField.SetFocus();
            }
            catch (Exception e)
            {
                Program.ShowError("Database Error", $"Could not save slip:\n{e.Message}");
            }
        }
    }
}