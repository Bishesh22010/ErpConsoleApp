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

            var listFrame = new FrameView("Existing Items") { X = 0, Y = 0, Width = Dim.Percent(30), Height = Dim.Fill(1) };
            itemList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            itemList.SelectedItemChanged += OnItemSelected;
            listFrame.Add(itemList);

            var editFrame = new FrameView("Add / Manage Item") { X = Pos.Right(listFrame), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1) };
            var container = new View() { X = Pos.Center(), Y = Pos.Center(), Width = 40, Height = 12 };

            container.Add(new Label("Item Name:") { X = 0, Y = 0 });
            itemNameField = new TextField("") { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = Colors.TextScheme };

            var saveButton = new Button("_Save New Item") { X = 0, Y = 3, Width = Dim.Fill(), ColorScheme = Colors.ButtonScheme };
            saveButton.Clicked += OnSave;

            var deleteButton = new Button("_Delete Selected") { X = 0, Y = 6, Width = Dim.Fill(), ColorScheme = Colors.ErrorScheme };
            deleteButton.Clicked += OnDelete;

            container.Add(itemNameField, saveButton, deleteButton);
            editFrame.Add(container);

            var closeButton = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            closeButton.Clicked += () => Application.RequestStop();

            Add(listFrame, editFrame, closeButton);
            LoadItems();
            itemNameField.SetFocus();
        }

        private void LoadItems()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    items = db.Items.OrderBy(i => i.ItemName).ToList();
                    itemList.SetSource(items.Select(i => i.ItemName).ToList());
                }
                selectedItem = null; itemNameField.Text = "";
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnItemSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < items.Count)
            {
                selectedItem = items[args.Item];
                itemNameField.Text = selectedItem.ItemName;
            }
        }

        private void OnSave()
        {
            string name = itemNameField.Text?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(name)) { Program.ShowError("Error", "Item Name required."); return; }

            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.Items.Any(i => i.ItemName.ToLower() == name.ToLower()))
                    {
                        Program.ShowError("Error", "Item already exists."); return;
                    }
                    db.Items.Add(new Item { ItemName = name });
                    db.SaveChanges();
                }
                LoadItems();
                if (Program.ShowQuery("Success", "Item added. Add another?"))
                {
                    itemNameField.Text = ""; itemNameField.SetFocus();
                }
                else { Application.RequestStop(); }
            }
            catch (Exception e) { Program.ShowError("Error", e.Message); }
        }

        private void OnDelete()
        {
            if (selectedItem == null) return;
            if (!Program.ShowQuery("Confirm", $"Delete '{selectedItem.ItemName}'?")) return;
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