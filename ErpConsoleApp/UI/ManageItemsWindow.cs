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

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Left Pane: List ---
            var listFrame = new FrameView("Existing Items")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(35),
                Height = Dim.Fill(2) // Leaves 2 rows at the bottom for Back button and Shortcuts
            };

            itemList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            itemList.SelectedItemChanged += OnItemSelected;
            listFrame.Add(itemList);

            // --- Right Pane: Add/Edit ---
            var editFrame = new FrameView("Add / Manage Item")
            {
                X = Pos.Right(listFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2) // Leaves 2 rows at the bottom for Back button and Shortcuts
            };

            var container = new View()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = 45,
                Height = 18
            };

            container.Add(new Label("Item Code:") { X = 0, Y = 0 });
            itemCodeField = new TextField("") { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = Colors.TextScheme };

            container.Add(new Label("Item Name:") { X = 0, Y = 3 });
            itemNameField = new TextField("") { X = 0, Y = 4, Width = Dim.Fill(), ColorScheme = Colors.TextScheme };

            var saveButton = new Button("_Save New Item") { X = 0, Y = 7, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            saveButton.Clicked += OnSave;

            var updateButton = new Button("_Update Selected") { X = 0, Y = 9, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            updateButton.Clicked += OnUpdate;

            var deleteButton = new Button("_Delete Selected") { X = 0, Y = 12, Width = Dim.Fill(), ColorScheme = Colors.ErrorScheme };
            deleteButton.Clicked += OnDelete;

            container.Add(itemCodeField, itemNameField, saveButton, updateButton, deleteButton);
            editFrame.Add(container);

            // Back Button at bottom (Moved up to AnchorEnd(2) to make room for shortcuts)
            var closeButton = new Button("_Back")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2),
                ColorScheme = Colors.ButtonScheme
            };
            closeButton.Clicked += () => Application.RequestStop();

            // --- NEW: App-wide Shortcut Display Pattern ---
            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save | [Alt+U] Update | [Alt+D] Delete | [Alt+B]/[ESC] Back | [Tab] Navigate")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1), // Placed at the very bottom
                ColorScheme = Colors.ResultScheme
            };

            Add(listFrame, editFrame, closeButton, shortcutsLabel);

            LoadItems();
            itemCodeField.SetFocus();
        }

        private void LoadItems()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    items = db.Items.OrderBy(i => i.ItemCode).ToList();
                    // Display both Code and Name in the list
                    itemList.SetSource(items.Select(i => $"[{i.ItemCode}] {i.ItemName}").ToList());
                }
                selectedItem = null;
                itemCodeField.Text = "";
                itemNameField.Text = "";
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnItemSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < items.Count)
            {
                selectedItem = items[args.Item];
                itemCodeField.Text = selectedItem.ItemCode;
                itemNameField.Text = selectedItem.ItemName;
            }
        }

        private void OnSave()
        {
            string code = itemCodeField.Text?.ToString().Trim() ?? "";
            string name = itemNameField.Text?.ToString().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                Program.ShowError("Error", "Both Item Code and Name are required."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.Items.Any(i => i.ItemCode.ToLower() == code.ToLower()))
                    {
                        Program.ShowError("Error", $"Item Code '{code}' is already assigned."); return;
                    }
                    db.Items.Add(new Item { ItemCode = code, ItemName = name });
                    db.SaveChanges();
                }

                LoadItems();

                if (Program.ShowQuery("Success", "Item added.\nAdd another?"))
                {
                    itemCodeField.Text = "";
                    itemNameField.Text = "";
                    itemCodeField.SetFocus();
                }
                else
                {
                    Application.RequestStop();
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnUpdate()
        {
            if (selectedItem == null) { Program.ShowError("Error", "Select an item to update."); return; }

            string code = itemCodeField.Text?.ToString().Trim() ?? "";
            string name = itemNameField.Text?.ToString().Trim() ?? "";

            try
            {
                using (var db = new AppDbContext())
                {
                    var item = db.Items.Find(selectedItem.ItemId);
                    if (item != null)
                    {
                        // Check if new code is taken by another item
                        if (db.Items.Any(i => i.ItemCode.ToLower() == code.ToLower() && i.ItemId != item.ItemId))
                        {
                            Program.ShowError("Error", "Item Code already in use."); return;
                        }
                        item.ItemCode = code;
                        item.ItemName = name;
                        db.SaveChanges();
                        Program.ShowMessage("Success", "Item updated.");
                        LoadItems();
                    }
                }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnDelete()
        {
            if (selectedItem == null) return;
            if (!Program.ShowQuery("Confirm", $"Delete item [{selectedItem.ItemCode}]?")) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var item = db.Items.Find(selectedItem.ItemId);
                    if (item != null) { db.Items.Remove(item); db.SaveChanges(); }
                }
                LoadItems();
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }
    }
}