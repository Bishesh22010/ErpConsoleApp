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
        private TextField nameField;
        private TextField gstField;
        private TextField phoneField;
        private TextView addressView; // Use TextView for multi-line address
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
                Height = Dim.Fill(2)
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
                Height = Dim.Fill(2)
            };

            // Party Name
            rightPane.Add(new Label("Party Name:") { X = 2, Y = 1 });
            nameField = new TextField("") { X = 18, Y = 1, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // GST Number
            rightPane.Add(new Label("GST Number:") { X = 2, Y = 3 });
            gstField = new TextField("") { X = 18, Y = 3, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // Phone Number
            rightPane.Add(new Label("Phone No:") { X = 2, Y = 5 });
            phoneField = new TextField("") { X = 18, Y = 5, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            // Address
            rightPane.Add(new Label("Address:") { X = 2, Y = 7 });
            addressView = new TextView()
            {
                X = 18,
                Y = 7,
                Width = Dim.Fill(2),
                Height = 3,
                ColorScheme = Colors.TextScheme,
                ReadOnly = false
            };

            // Buttons
            var btnSave = new Button("_Save as New") { X = Pos.Center(), Y = 12, ColorScheme = Colors.ButtonScheme };
            var btnUpdate = new Button("_Update Selected") { X = Pos.Center(), Y = 14, ColorScheme = Colors.ButtonScheme };
            var btnDelete = new Button("_Delete Selected") { X = Pos.Center(), Y = 16, ColorScheme = Colors.ErrorScheme };

            btnSave.Clicked += OnSaveNew;
            btnUpdate.Clicked += OnUpdateSelected;
            btnDelete.Clicked += OnDeleteSelected;

            rightPane.Add(nameField, gstField, phoneField, addressView, btnSave, btnUpdate, btnDelete);

            // Back Button at bottom
            var btnBack = new Button("_Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                ColorScheme = Colors.ButtonScheme
            };
            btnBack.Clicked += () => Application.RequestStop();

            Add(leftPane, rightPane, btnBack);
            RefreshList();
        }

        private void RefreshList()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    parties = db.Parties.OrderBy(p => p.Name).ToList();
                    partyListView.SetSource(parties.Select(p => p.Name).ToList());
                }
                ClearFields();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void ClearFields()
        {
            selectedParty = null;
            nameField.Text = "";
            gstField.Text = "";
            phoneField.Text = "";
            addressView.Text = "";
        }

        private void OnPartySelectionChanged(ListViewItemEventArgs args)
        {
            if (args.Item < 0 || args.Item >= parties.Count) return;
            selectedParty = parties[args.Item];
            nameField.Text = selectedParty.Name;
            gstField.Text = selectedParty.GstNumber ?? "";
            phoneField.Text = selectedParty.PhoneNumber ?? "";
            addressView.Text = selectedParty.Address ?? "";
        }

        private void OnSaveNew()
        {
            if (string.IsNullOrWhiteSpace(nameField.Text.ToString()))
            {
                Program.ShowError("Validation", "Name is required."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Parties.Add(new Party
                    {
                        Name = nameField.Text.ToString(),
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

            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null)
                    {
                        p.Name = nameField.Text.ToString();
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