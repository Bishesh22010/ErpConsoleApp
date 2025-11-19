using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ManagePartiesWindow : Window
    {
        private ListView partyList;
        private TextField partyNameField;
        private List<Party> parties = new List<Party>();
        private Party selectedParty = null;

        public ManagePartiesWindow() : base("Manage Parties (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;

            // --- FULL SCREEN SETTINGS ---
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();

            // --- ESCAPE KEY HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc)
                {
                    Application.RequestStop();
                    e.Handled = true;
                }
            };

            // --- Left Pane: List (Takes 30% width) ---
            var listFrame = new FrameView("Existing Parties")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(30),
                Height = Dim.Fill(1) // Leave space for footer
            };

            partyList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            partyList.SelectedItemChanged += OnPartySelected;
            listFrame.Add(partyList);

            // --- Right Pane: Actions (Takes remaining 70%) ---
            var editFrame = new FrameView("Add / Edit Party")
            {
                X = Pos.Right(listFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };

            // Center the controls inside the right pane
            var innerContainer = new View()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = 40,
                Height = 15
            };

            innerContainer.Add(new Label("Party Name:") { X = 0, Y = 0 });
            partyNameField = new TextField("")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };

            var saveNewButton = new Button("_Save as New")
            {
                X = 0,
                Y = 3,
                Width = Dim.Fill(),
                ColorScheme = Colors.ButtonScheme
            };
            saveNewButton.Clicked += OnSaveNew;

            var updateButton = new Button("_Update Selected")
            {
                X = 0,
                Y = 5,
                Width = Dim.Fill(),
                ColorScheme = Colors.ButtonScheme
            };
            updateButton.Clicked += OnUpdate;

            var deleteButton = new Button("_Delete Selected")
            {
                X = 0,
                Y = 8,
                Width = Dim.Fill(),
                ColorScheme = Colors.ErrorScheme
            };
            deleteButton.Clicked += OnDelete;

            innerContainer.Add(partyNameField, saveNewButton, updateButton, deleteButton);
            editFrame.Add(innerContainer);

            // --- Close Button (Footer) ---
            var closeButton = new Button("_Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            closeButton.Clicked += () => Application.RequestStop();

            Add(listFrame, editFrame, closeButton);

            LoadParties();
            partyNameField.SetFocus();
        }
        private void LoadParties()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    parties = db.Parties.OrderBy(p => p.Name).ToList();
                    partyList.SetSource(parties.Select(p => p.Name).ToList());
                }
                selectedParty = null;
                partyNameField.Text = "";
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnPartySelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < parties.Count)
            {
                selectedParty = parties[args.Item];
                partyNameField.Text = selectedParty.Name;
            }
            else
            {
                selectedParty = null;
                partyNameField.Text = "";
            }
        }

        private void OnSaveNew()
        {
            string name = partyNameField.Text?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(name)) { Program.ShowError("Error", "Name required."); return; }
            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.Parties.Any(p => p.Name.ToLower() == name.ToLower()))
                    {
                        Program.ShowError("Error", "Party exists."); return;
                    }
                    db.Parties.Add(new Party { Name = name });
                    db.SaveChanges();
                }
                LoadParties();
                partyNameField.SetFocus();
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnUpdate()
        {
            if (selectedParty == null) return;
            string name = partyNameField.Text?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                using (var db = new AppDbContext())
                {
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null) { p.Name = name; db.SaveChanges(); }
                }
                LoadParties();
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnDelete()
        {
            if (selectedParty == null) return;
            if (!Program.ShowQuery("Confirm", $"Delete {selectedParty.Name}?")) return;
            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.PurchaseSlips.Any(s => s.PartyId == selectedParty.PartyId))
                    {
                        Program.ShowError("Error", "Party has slips."); return;
                    }
                    var p = db.Parties.Find(selectedParty.PartyId);
                    if (p != null) { db.Parties.Remove(p); db.SaveChanges(); }
                }
                LoadParties();
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }
    }
}