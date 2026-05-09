using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ManageItemsWindow : Window
    {
        private ListView itemList;
        private TextField itemCodeField;
        private TextField itemNameField;
        private List<Item> items = new List<Item>();
        private Item selectedItem = null;

        public ManageItemsWindow() : base("Manage Items (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            var listFrame = new FrameView("Existing Items") { X = 0, Y = 0, Width = Dim.Percent(35), Height = Dim.Fill(2) };
            itemList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            itemList.SelectedItemChanged += OnItemSelected;
            listFrame.Add(itemList);

            var editFrame = new FrameView("Add / Manage Item") { X = Pos.Right(listFrame), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2) };

            var container = new View() { X = Pos.Center(), Y = Pos.Center(), Width = 45, Height = 18 };

            container.Add(new Label("Item Code:") { X = 0, Y = 0 });
            itemCodeField = new TextField("") { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = Colors.TextScheme };

            container.Add(new Label("Item Name:") { X = 0, Y = 3 });
            itemNameField = new TextField("") { X = 0, Y = 4, Width = Dim.Fill(), ColorScheme = Colors.TextScheme };

            itemCodeField.KeyDown += (e) => { if (e.KeyEvent.Key == Key.Enter) { e.Handled = true; itemNameField.SetFocus(); } };
            itemNameField.KeyDown += (e) => { if (e.KeyEvent.Key == Key.Enter) { e.Handled = true; OnSave(); } };

            var saveButton = new Button("_Save New Item") { X = 0, Y = 7, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            saveButton.Clicked += OnSave;

            var updateButton = new Button("_Update Selected") { X = 0, Y = 9, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            updateButton.Clicked += OnUpdate;

            var deleteButton = new Button("_Delete Selected") { X = 0, Y = 11, Width = Dim.Fill(), ColorScheme = Colors.ErrorScheme };
            deleteButton.Clicked += OnDelete;

            // --- NEW UNDO BUTTON ---
            var undoButton = new Button("U_ndo (Ctrl+Z)") { X = 0, Y = 13, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            undoButton.Clicked += UndoManager.Undo;

            container.Add(itemCodeField, itemNameField, saveButton, updateButton, deleteButton, undoButton);
            editFrame.Add(container);

            var closeButton = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            closeButton.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save | [Alt+U] Update | [Alt+D] Delete | [Ctrl+Z] Undo | [Alt+B] Back")
            { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ResultScheme };

            Add(listFrame, editFrame, closeButton, shortcutsLabel);
            LoadItems(); itemCodeField.SetFocus();
        }

        private void LoadItems()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    items = db.Items.OrderBy(i => i.ItemCode).ToList();
                    itemList.SetSource(items.Select(i => $"[{i.ItemCode}] {i.ItemName}").ToList());
                }
                selectedItem = null; itemCodeField.Text = ""; itemNameField.Text = "";
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnItemSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < items.Count)
            {
                selectedItem = items[args.Item]; itemCodeField.Text = selectedItem.ItemCode; itemNameField.Text = selectedItem.ItemName;
            }
        }

        private void OnSave()
        {
            string code = itemCodeField.Text?.ToString().Trim() ?? "";
            string name = itemNameField.Text?.ToString().Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) { MessageBox.Query("Error", "Item Code and Name required.", "Ok"); return; }

            try
            {
                int newItemId;
                using (var db = new AppDbContext())
                {
                    if (db.Items.Any(i => i.ItemCode.ToLower() == code.ToLower())) { MessageBox.Query("Error", $"Item Code '{code}' assigned.", "Ok"); return; }
                    var newItem = new Item { ItemCode = code, ItemName = name };
                    db.Items.Add(newItem);
                    db.SaveChanges();
                    newItemId = newItem.ItemId;
                }

                // --- PUSH TO UNDO MANAGER ---
                UndoManager.Push($"Added Item: {name}", () => {
                    using (var dbUndo = new AppDbContext())
                    {
                        var i = dbUndo.Items.Find(newItemId);
                        if (i != null) { dbUndo.Items.Remove(i); dbUndo.SaveChanges(); }
                    }
                }, LoadItems);

                LoadItems();
                if (MessageBox.Query("Success", "Item added.\nAdd another?", "Yes", "No") == 0) { itemCodeField.SetFocus(); }
                else { Application.RequestStop(); }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnUpdate()
        {
            if (selectedItem == null) { MessageBox.Query("Error", "Select an item.", "Ok"); return; }
            string code = itemCodeField.Text?.ToString().Trim() ?? ""; string name = itemNameField.Text?.ToString().Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) { MessageBox.Query("Error", "Item Code and Name required.", "Ok"); return; }

            try
            {
                using (var db = new AppDbContext())
                {
                    var item = db.Items.Find(selectedItem.ItemId);
                    if (item != null)
                    {
                        if (db.Items.Any(i => i.ItemCode.ToLower() == code.ToLower() && i.ItemId != item.ItemId)) { MessageBox.Query("Error", "Item Code in use.", "Ok"); return; }

                        var oldState = new { item.ItemId, item.ItemCode, item.ItemName };
                        item.ItemCode = code; item.ItemName = name;
                        db.SaveChanges();

                        // --- PUSH TO UNDO MANAGER ---
                        UndoManager.Push($"Updated Item: {oldState.ItemName}", () => {
                            using (var dbUndo = new AppDbContext())
                            {
                                var iUndo = dbUndo.Items.Find(oldState.ItemId);
                                if (iUndo != null)
                                {
                                    iUndo.ItemCode = oldState.ItemCode; iUndo.ItemName = oldState.ItemName; dbUndo.SaveChanges();
                                }
                            }
                        }, LoadItems);

                        MessageBox.Query("Success", "Item updated.", "Ok"); LoadItems();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnDelete()
        {
            string code = itemCodeField.Text?.ToString().Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code)) { MessageBox.Query("Error", "Enter an Item Code to delete.", "Ok"); return; }
            if (MessageBox.Query("Confirm", $"Are you sure you want to delete item [{code}]?", "Yes", "No") != 0) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var item = db.Items.FirstOrDefault(i => i.ItemCode.ToLower() == code.ToLower());
                    if (item != null)
                    {
                        var backupItem = new Item { ItemCode = item.ItemCode, ItemName = item.ItemName };
                        db.Items.Remove(item); db.SaveChanges();

                        // --- PUSH TO UNDO MANAGER ---
                        UndoManager.Push($"Deleted Item: {backupItem.ItemName}", () => {
                            using (var dbUndo = new AppDbContext())
                            {
                                dbUndo.Items.Add(backupItem); dbUndo.SaveChanges();
                            }
                        }, LoadItems);

                        MessageBox.Query("Success", "Item deleted.", "Ok");
                    }
                    else { MessageBox.Query("Error", $"Item '{code}' not found.", "Ok"); }
                }
                LoadItems(); itemCodeField.SetFocus();
            }
            catch (Exception e) { Program.ShowError("Error", e.InnerException?.Message ?? e.Message); }
        }
    }
}