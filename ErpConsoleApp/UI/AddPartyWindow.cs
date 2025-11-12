using System;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// Modal window for adding a new Party to the database.
    /// </summary>
    public class AddPartyWindow : Window
    {
        private TextField partyNameField;

        public AddPartyWindow() : base("Add New Party")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center();
            Y = Pos.Center() - 3;
            Width = 50;
            Height = 10;
            Modal = true;

            var nameLabel = new Label("New Party Name:")
            {
                X = 2,
                Y = 2
            };

            partyNameField = new TextField("")
            {
                X = 20,
                Y = 2,
                Width = 25,
                ColorScheme = Colors.TextScheme
            };
            partyNameField.SetFocus();

            var saveButton = new Button("_Save")
            {
                X = Pos.Center() - 10,
                Y = 6,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            saveButton.Clicked += OnSave;

            var cancelButton = new Button("_Cancel")
            {
                X = Pos.Center() + 2,
                Y = 6,
                ColorScheme = Colors.ButtonScheme
            };
            cancelButton.Clicked += () => Application.RequestStop();

            Add(nameLabel, partyNameField, saveButton, cancelButton);
        }

        private void OnSave()
        {
            string partyName = partyNameField.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(partyName))
            {
                Program.ShowError("Validation Error", "Party name cannot be empty.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Check if party already exists
                    var existing = db.Parties.FirstOrDefault(p => p.Name.ToLower() == partyName.ToLower());
                    if (existing != null)
                    {
                        Program.ShowError("Error", $"Party '{partyName}' already exists.");
                        return;
                    }

                    // Add and save new party
                    var newParty = new Party { Name = partyName };
                    db.Parties.Add(newParty);
                    db.SaveChanges();
                }

                Program.ShowMessage("Success", $"Party '{partyName}' was added successfully.");
                partyNameField.Text = ""; // Clear for next entry
                partyNameField.SetFocus();
            }
            catch (Exception e)
            {
                Program.ShowError("Database Error", $"Could not save new party:\n{e.Message}");
            }
        }
    }
}