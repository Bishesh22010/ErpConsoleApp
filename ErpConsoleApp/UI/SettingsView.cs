using System;
using System.IO;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// A View component for Settings, designed to be embedded in the MenuWindow.
    /// </summary>
    public class SettingsView : View
    {
        private TextField currentPinField;
        private TextField newPinField;
        private TextField confirmPinField;

        public SettingsView()
        {
            // Fill the parent container
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();

            // --- TOP: CHANGE PIN ---
            var pinFrame = new FrameView("Change Login PIN")
            {
                X = Pos.Center(),
                Y = 2, // A bit of padding from top
                Width = 50,
                Height = 12
            };

            pinFrame.Add(new Label("Current PIN:") { X = 2, Y = 1 });
            currentPinField = new TextField("") { X = 18, Y = 1, Width = 20, Secret = true, ColorScheme = Colors.TextScheme };

            pinFrame.Add(new Label("New PIN:") { X = 2, Y = 3 });
            newPinField = new TextField("") { X = 18, Y = 3, Width = 20, Secret = true, ColorScheme = Colors.TextScheme };

            pinFrame.Add(new Label("Confirm PIN:") { X = 2, Y = 5 });
            confirmPinField = new TextField("") { X = 18, Y = 5, Width = 20, Secret = true, ColorScheme = Colors.TextScheme };

            var btnChangePin = new Button("Update _PIN") { X = Pos.Center(), Y = 8, ColorScheme = Colors.ButtonScheme };
            btnChangePin.Clicked += ChangePin;

            pinFrame.Add(currentPinField, newPinField, confirmPinField, btnChangePin);

            // --- BOTTOM: BACKUP & RESTORE ---
            var backupFrame = new FrameView("Database Maintenance")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(pinFrame) + 1,
                Width = 50,
                Height = 8
            };

            var btnBackup = new Button("Create _Backup") { X = 5, Y = 2, ColorScheme = Colors.ButtonScheme };
            btnBackup.Clicked += BackupDatabase;

            var btnRestore = new Button("_Restore Backup") { X = Pos.AnchorEnd(20), Y = 2, ColorScheme = Colors.ErrorScheme };
            btnRestore.Clicked += RestoreDatabase;

            backupFrame.Add(btnBackup, btnRestore);

            Add(pinFrame, backupFrame);
        }

        private void ChangePin()
        {
            string current = currentPinField.Text.ToString();
            string newPin = newPinField.Text.ToString();
            string confirm = confirmPinField.Text.ToString();

            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPin))
            {
                Program.ShowError("Error", "All fields are required."); return;
            }

            if (newPin != confirm)
            {
                Program.ShowError("Error", "New PINs do not match."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var setting = db.Settings.Find("LoginPin");
                    if (setting == null)
                    {
                        setting = new AppSetting { Key = "LoginPin", Value = "1234" };
                        db.Settings.Add(setting);
                    }

                    if (setting.Value != current)
                    {
                        Program.ShowError("Error", "Incorrect Current PIN."); return;
                    }

                    setting.Value = newPin;
                    db.SaveChanges();
                    Program.ShowMessage("Success", "PIN updated successfully.");

                    currentPinField.Text = "";
                    newPinField.Text = "";
                    confirmPinField.Text = "";
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void BackupDatabase()
        {
            var saveDialog = new SaveDialog("Backup Database", "Select location for backup");
            string defaultFileName = $"erp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            Directory.CreateDirectory(backupDir);

            // Correct way to set default path/filename in Terminal.Gui
            saveDialog.FilePath = Path.Combine(backupDir, defaultFileName);

            saveDialog.AllowedFileTypes = new[] { "db" };

            Application.Run(saveDialog);

            if (!saveDialog.Canceled && saveDialog.FilePath != null)
            {
                string destPath = saveDialog.FilePath.ToString();

                if (!destPath.EndsWith(".db")) destPath += ".db";

                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../erp.db");

                try
                {
                    File.Copy(sourcePath, destPath, true);
                    Program.ShowMessage("Success", $"Backup saved to:\n{destPath}");
                }
                catch (Exception e)
                {
                    Program.ShowError("Backup Failed", e.Message + "\n\n(Ensure source DB path is correct)");
                }
            }
        }

        private void RestoreDatabase()
        {
            if (!Program.ShowQuery("WARNING", "Restore will OVERWRITE current data.\nAre you sure?")) return;

            var openDialog = new OpenDialog("Select Backup File", "Restore");
            openDialog.DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            openDialog.AllowedFileTypes = new[] { "db" };

            Application.Run(openDialog);

            if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
            {
                string sourceFile = openDialog.FilePaths[0];
                string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../erp.db");

                try
                {
                    File.Copy(sourceFile, destPath, true);
                    Program.ShowMessage("Success", "Database Restored.\nPlease restart the application.");
                    // In a real app, you might trigger a restart here
                }
                catch (Exception e)
                {
                    Program.ShowError("Restore Failed", $"Could not restore:\n{e.Message}\n\n(Make sure no other app is using the DB)");
                }
            }
        }
    }
}