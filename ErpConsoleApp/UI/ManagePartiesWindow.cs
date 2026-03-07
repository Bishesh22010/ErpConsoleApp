using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ManagePartiesWindow : Window
    {
        private ListView partyListView;
        private TextField partyCodeField;
        private TextField nameField;
        private TextField gstField;
        private TextField phoneField;
        private TextView addressView;
        private List<Party> parties = new List<Party>();
        private Party selectedParty = null;

        public ManagePartiesWindow() : base("Manage Parties (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Left Pane: List ---
            var leftPane = new FrameView("Existing Parties")
            {
                X = 0,
                Y = 0,
                Width = 30,
                Height = Dim.Fill(2) // Leaves 2 rows at the bottom for Back button and Shortcuts
            };
            partyListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            partyListView.SelectedItemChanged += OnPartySelectionChanged;
            leftPane.Add(partyListView);

            // --- Right Pane: Edit Form ---
            var rightPane = new FrameView("Add / Edit Party")
            {
                X = 30,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2) // Leaves 2 rows at the bottom for Back button and Shortcuts
            };

            // Party ID (Code)
            rightPane.Add(new Label("Party ID:") { X = 2, Y = 1 });
            partyCodeField = new TextField("") { X = 18, Y = 1, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // Party Name
            rightPane.Add(new Label("Party Name:") { X = 2, Y = 3 });
            nameField = new TextField("") { X = 18, Y = 3, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // GST Number
            rightPane.Add(new Label("GST Number:") { X = 2, Y = 5 });
            gstField = new TextField("") { X = 18, Y = 5, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // Phone Number
            rightPane.Add(new Label("Phone No:") { X = 2, Y = 7 });
            phoneField = new TextField("") { X = 18, Y = 7, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // Address
            rightPane.Add(new Label("Address:") { X = 2, Y = 9 });
            addressView = new TextView()
            {
                X = 18,
                Y = 9,
                Width = Dim.Fill(2),
                Height = 3,
                ColorScheme = Colors.TextScheme,
                ReadOnly = false
            };

            // Buttons - Adjusted Y spacing to ensure they don't overlap fields
            var btnSave = new Button("_Save as New") { X = Pos.Center() - 15, Y = 14, ColorScheme = Colors.ButtonScheme };
            var btnUpdate = new Button("_Update Selected") { X = Pos.Center() + 5, Y = 14, ColorScheme = Colors.ButtonScheme };
            var btnDelete = new Button("_Delete Selected") { X = Pos.Center(), Y = 16, ColorScheme = Colors.ErrorScheme };

            btnSave.Clicked += OnSaveNew;
            btnUpdate.Clicked += OnUpdateSelected;
            btnDelete.Clicked += OnDeleteSelected;

            rightPane.Add(partyCodeField, nameField, gstField, phoneField, addressView, btnSave, btnUpdate, btnDelete);

            // Back Button at bottom (Moved up to AnchorEnd(2) to make room for shortcuts)
            var btnBack = new Button("_Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2),
                ColorScheme = Colors.ButtonScheme
            };
            btnBack.Clicked += () => Application.RequestStop();

            // --- NEW: App-wide Shortcut Display Pattern ---
            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save | [Alt+U] Update | [Alt+D] Delete | [Alt+B]/[ESC] Back | [Tab] Navigate")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1), // Placed at the very bottom
                ColorScheme = Colors.ResultScheme
            };

            Add(leftPane, rightPane, btnBack, shortcutsLabel);
            RefreshList();
        }

        private void RefreshList()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    parties = db.Parties.OrderBy(p => p.PartyCode).ToList();
                    partyListView.SetSource(parties.Select(p => $"[{p.PartyCode}] {p.Name}").ToList());
                }
                ClearFields();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void ClearFields()
        {
            selectedParty = null;
            partyCodeField.Text = "";
            nameField.Text = "";
            gstField.Text = "";
            phoneField.Text = "";
            addressView.Text = "";
        }

        private void OnPartySelectionChanged(ListViewItemEventArgs args)
        {
            if (args.Item < 0 || args.Item >= parties.Count) return;
            selectedParty = parties[args.Item];
            partyCodeField.Text = selectedParty.PartyCode ?? "";
            nameField.Text = selectedParty.Name;
            gstField.Text = selectedParty.GstNumber ?? "";
            phoneField.Text = selectedParty.PhoneNumber ?? "";
            addressView.Text = selectedParty.Address ?? "";
        }

        private void OnSaveNew()
        {
            string code = partyCodeField.Text?.ToString().Trim() ?? "";
            string name = nameField.Text?.ToString().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                Program.ShowError("Validation", "Party ID and Name are required."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.Parties.Any(p => p.PartyCode.ToLower() == code.ToLower()))
                    {
                        Program.ShowError("Error", $"Party ID '{code}' is already assigned."); return;
                    }

                    db.Parties.Add(new Party
                    {
                        PartyCode = code,
                        Name = name,
                        GstNumber = gstField.Text.ToString(),
                        PhoneNumber = phoneField.Text.ToString(),
                        Address = addressView.Text.ToString()
                    });
                    db.SaveChanges();
                }
                Program.ShowMessage("Success", "New Party added.");
                RefreshList();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnUpdateSelected()
        {
            if (selectedParty == null) { Program.ShowError("Error", "Select a party first."); return; }

            string code = partyCodeField.Text?.ToString().Trim() ?? "";
            string name = nameField.Text?.ToString().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                Program.ShowError("Validation", "Party ID and Name are required."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null)
                    {
                        if (db.Parties.Any(x => x.PartyCode.ToLower() == code.ToLower() && x.PartyId != p.PartyId))
                        {
                            Program.ShowError("Error", "Party ID already in use."); return;
                        }

                        p.PartyCode = code;
                        p.Name = name;
                        p.GstNumber = gstField.Text.ToString();
                        p.PhoneNumber = phoneField.Text.ToString();
                        p.Address = addressView.Text.ToString();
                        db.SaveChanges();
                        Program.ShowMessage("Success", "Party updated.");
                        RefreshList();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnDeleteSelected()
        {
            if (selectedParty == null) return;
            if (!Program.ShowQuery("Confirm Delete", $"Are you sure you want to delete {selectedParty.Name}?")) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null)
                    {
                        db.Parties.Remove(p);
                        db.SaveChanges();
                        Program.ShowMessage("Success", "Party deleted.");
                        RefreshList();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}