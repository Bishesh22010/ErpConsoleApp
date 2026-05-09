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

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            var leftPane = new FrameView("Existing Parties") { X = 0, Y = 0, Width = 30, Height = Dim.Fill(2) };
            partyListView = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            partyListView.SelectedItemChanged += OnPartySelectionChanged;
            leftPane.Add(partyListView);

            var rightPane = new FrameView("Add / Edit Party") { X = 30, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2) };

            rightPane.Add(new Label("Party ID:") { X = 2, Y = 1 });
            partyCodeField = new TextField("") { X = 18, Y = 1, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            rightPane.Add(new Label("Party Name:") { X = 2, Y = 3 });
            nameField = new TextField("") { X = 18, Y = 3, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            rightPane.Add(new Label("GST Number:") { X = 2, Y = 5 });
            gstField = new TextField("") { X = 18, Y = 5, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            rightPane.Add(new Label("Phone No:") { X = 2, Y = 7 });
            phoneField = new TextField("") { X = 18, Y = 7, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };

            rightPane.Add(new Label("Address:") { X = 2, Y = 9 });
            addressView = new TextView() { X = 18, Y = 9, Width = Dim.Fill(2), Height = 3, ColorScheme = Colors.TextScheme, ReadOnly = false, AllowsTab = false };

            var btnSave = new Button("_Save as New") { X = Pos.Center() - 15, Y = 14, ColorScheme = Colors.ButtonScheme };
            var btnUpdate = new Button("_Update Selected") { X = Pos.Center() + 5, Y = 14, ColorScheme = Colors.ButtonScheme };

            var btnDelete = new Button("_Delete Selected") { X = Pos.Center() - 15, Y = 16, ColorScheme = Colors.ErrorScheme };
            var btnUndo = new Button("U_ndo (Ctrl+Z)") { X = Pos.Center() + 5, Y = 16, ColorScheme = Colors.ButtonScheme };

            btnSave.Clicked += OnSaveNew;
            btnUpdate.Clicked += OnUpdateSelected;
            btnDelete.Clicked += OnDeleteSelected;
            btnUndo.Clicked += UndoManager.Undo;

            rightPane.Add(partyCodeField, nameField, gstField, phoneField, addressView, btnSave, btnUpdate, btnDelete, btnUndo);

            var btnBack = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnBack.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save | [Alt+U] Update | [Alt+D] Delete | [Ctrl+Z] Undo | [Alt+B] Back")
            { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ResultScheme };

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
            selectedParty = null; partyCodeField.Text = ""; nameField.Text = ""; gstField.Text = ""; phoneField.Text = ""; addressView.Text = "";
        }

        private void OnPartySelectionChanged(ListViewItemEventArgs args)
        {
            if (args.Item < 0 || args.Item >= parties.Count) return;
            selectedParty = parties[args.Item];
            partyCodeField.Text = selectedParty.PartyCode ?? ""; nameField.Text = selectedParty.Name;
            gstField.Text = selectedParty.GstNumber ?? ""; phoneField.Text = selectedParty.PhoneNumber ?? "";
            addressView.Text = selectedParty.Address ?? "";
        }

        private bool ValidateInput(out string code, out string name, out string phone, out string gst)
        {
            code = partyCodeField.Text?.ToString().Trim() ?? ""; name = nameField.Text?.ToString().Trim() ?? "";
            phone = phoneField.Text?.ToString().Trim() ?? ""; gst = gstField.Text?.ToString().Trim().ToUpper() ?? "";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) { MessageBox.Query("Validation", "Party ID and Name required.", "Ok"); return false; }
            if (string.IsNullOrWhiteSpace(phone) || phone.Length != 10 || !phone.All(char.IsDigit)) { MessageBox.Query("Validation", "Mobile must be 10 digits.", "Ok"); return false; }
            if (string.IsNullOrWhiteSpace(gst)) { gst = "URP"; }
            return true;
        }

        private void OnSaveNew()
        {
            if (!ValidateInput(out string code, out string name, out string phone, out string gst)) return;

            try
            {
                int newPartyId;
                using (var db = new AppDbContext())
                {
                    if (db.Parties.Any(p => p.PartyCode.ToLower() == code.ToLower())) { MessageBox.Query("Error", $"Party ID '{code}' is already assigned.", "Ok"); return; }
                    var newParty = new Party { PartyCode = code, Name = name, GstNumber = gst, PhoneNumber = phone, Address = addressView.Text.ToString() };
                    db.Parties.Add(newParty);
                    db.SaveChanges();
                    newPartyId = newParty.PartyId;
                }

                // --- PUSH TO UNDO MANAGER ---
                UndoManager.Push($"Added Party: {name}", () => {
                    using (var dbUndo = new AppDbContext())
                    {
                        var p = dbUndo.Parties.Find(newPartyId);
                        if (p != null) { dbUndo.Parties.Remove(p); dbUndo.SaveChanges(); }
                    }
                }, RefreshList);

                MessageBox.Query("Success", "New Party added.", "Ok");
                RefreshList();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnUpdateSelected()
        {
            if (selectedParty == null) { MessageBox.Query("Error", "Select a party first.", "Ok"); return; }
            if (!ValidateInput(out string code, out string name, out string phone, out string gst)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null)
                    {
                        if (db.Parties.Any(x => x.PartyCode.ToLower() == code.ToLower() && x.PartyId != p.PartyId)) { MessageBox.Query("Error", "Party ID already in use.", "Ok"); return; }

                        // Capture old state for Undo
                        var oldState = new { p.PartyId, p.PartyCode, p.Name, p.GstNumber, p.PhoneNumber, p.Address };

                        p.PartyCode = code; p.Name = name; p.GstNumber = gst; p.PhoneNumber = phone; p.Address = addressView.Text.ToString();
                        db.SaveChanges();

                        // --- PUSH TO UNDO MANAGER ---
                        UndoManager.Push($"Updated Party: {oldState.Name}", () => {
                            using (var dbUndo = new AppDbContext())
                            {
                                var pUndo = dbUndo.Parties.Find(oldState.PartyId);
                                if (pUndo != null)
                                {
                                    pUndo.PartyCode = oldState.PartyCode; pUndo.Name = oldState.Name;
                                    pUndo.GstNumber = oldState.GstNumber; pUndo.PhoneNumber = oldState.PhoneNumber;
                                    pUndo.Address = oldState.Address; dbUndo.SaveChanges();
                                }
                            }
                        }, RefreshList);

                        MessageBox.Query("Success", "Party updated.", "Ok"); RefreshList();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnDeleteSelected()
        {
            if (selectedParty == null) return;
            if (MessageBox.Query("Confirm Delete", $"Are you sure you want to delete {selectedParty.Name}?", "Yes", "No") != 0) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null)
                    {
                        // Capture old state to restore later if undone
                        var backupParty = new Party { PartyCode = p.PartyCode, Name = p.Name, GstNumber = p.GstNumber, PhoneNumber = p.PhoneNumber, Address = p.Address };

                        db.Parties.Remove(p);
                        db.SaveChanges();

                        // --- PUSH TO UNDO MANAGER ---
                        UndoManager.Push($"Deleted Party: {backupParty.Name}", () => {
                            using (var dbUndo = new AppDbContext())
                            {
                                dbUndo.Parties.Add(backupParty); dbUndo.SaveChanges();
                            }
                        }, RefreshList);

                        MessageBox.Query("Success", "Party deleted.", "Ok"); RefreshList();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.InnerException?.Message ?? e.Message); }
        }
    }
}