using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database; // <-- THIS IS THE REQUIRED LINE
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

            // REMOVED: This was a confusing test entry.
            // if (partyNames.Count == 0)
            // {
            //     partyNames.Add("XYZ Party");
            // }
        }

        /// <summary>
        /// Validates the form and saves the new Purchase Slip to the database.
        /// </summary>
        private void OnGenerateSlip()
        {
            // --- 1. Validation ---

            // --- FIXED: Make reads from text fields "null-safe" ---
            // Use ?.ToString() ?? "" to safely get the text or an empty string.
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
                // Use a fresh DbContext for this "transaction"
                using (var db = new AppDbContext())
                {
                    // --- MODIFIED LOGIC ---
                    // Find the party. DO NOT create it.
                    Party party = db.Parties.FirstOrDefault(p => p.Name == partyName);

                    if (party == null)
                    {
                        // Party doesn't exist, show an error.
                        Program.ShowError("Party Not Found",
                            $"Party '{partyName}' does not exist.\n\n" +
                            "Please add it first using the 'Add & Delete Party' option.");
                        return; // Stop execution
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

                    // Save all changes (slip only)
                    db.SaveChanges();
                }

                // --- 3. Success ---
                Program.ShowMessage("Success", $"Purchase Slip generated for {partyName}\nAmount: {amount:C}");

                // Update the ComboBox list in case we added a new party
                // --- REMOVED THIS LOGIC ---
                // (We no longer add parties from this screen)

                // Clear form for next entry (but keep date and party)
                itemField.Text = "";
                amountField.Text = "";
                itemField.SetFocus();
            }
            catch (Exception e)
            {
                // --- MODIFIED ERROR MESSAGE ---
                string error = $"Could not save slip:\n{e.Message}\n\n" +
                               "Tip: Is 'erp.db' locked by another program (like DB Browser)?";
                Program.ShowError("Database Error", error);
            }
        }
    }
}