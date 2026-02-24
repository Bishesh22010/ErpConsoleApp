using Microsoft.EntityFrameworkCore;
using System;
using Terminal.Gui;
using ErpConsoleApp.Database; // <-- THIS IS THE REQUIRED LINE
using ErpConsoleApp.UI;     // Import UI classes
using System.Linq;


namespace ErpConsoleApp
{
    // --- Main Application Class ---
    // This class holds the Main method, navigation, and dialog helpers.
    class Program
    {
        static int Main(string[] args)
        {

            // Apply database migrations on startup
            try
            {
                // Use a fresh DbContext for this operation
                using (var db = new AppDbContext())
                {
                    db.Database.Migrate();
                }
            }
            catch (Exception e)
            {
                // If the DB fails to init, we must stop.
                Console.WriteLine($"Database initialization failed: {e.Message}");
                return 1; // Return an error code
            }

            Application.Init();

            // Apply the base color scheme to the entire application
            Application.Top.ColorScheme = Colors.BaseScheme;

            // Start by adding the Login Window
            Application.Top.Add(new LoginWindow());

            // Run the application
            Application.Run();
            Application.Shutdown();
            return 0;
        }

        // --- Navigation ---

        /// <summary>
        /// This is our simple "navigation" system.
        /// It removes all windows and shows the main menu.
        /// </summary>
        public static void ShowMenuPage()
        {
            Application.Top.RemoveAll();

            // Add the top-level menu bar
            Application.Top.Add(new AppMenuBar());

            // Add the main window
            Application.Top.Add(new MenuWindow());
            Application.Top.KeyPress += (args) =>
            {
                var key = args.KeyEvent.Key;

                if (key == (Key)'1')
                {
                    Program.OpenModal(new SettingsWindow());
                    args.Handled = true;
                }
                else if (key == (Key)'2')
                {
                    Program.OpenModal(new PurchaseWindow());
                    args.Handled = true;
                }
                else if (key == (Key)'3')
                {
                    Program.OpenModal(new SalaryWindow());
                    args.Handled = true;
                }
            };

        }

        /// <summary>
        /// Navigation method to go back to the login screen.
        /// </summary>
        public static void ShowLoginPage()
        {
            Application.Top.RemoveAll();
            Application.Top.Add(new LoginWindow());
        }

        // --- Custom Dialogs ---

        /// <summary>
        /// Shows a custom, themed error message.
        /// </summary>
        public static void ShowError(string title, string message)
        {
            var dialog = new Dialog(title, 60, 15) // <-- FIXED: Height increased from 10 to 15
            {
                ColorScheme = Colors.ErrorScheme
            };

            var msgLabel = new Label(message)
            {
                X = 2,
                Y = 2,
                TextAlignment = TextAlignment.Centered,
                Width = Dim.Fill(2),  // <-- ADDED: Allow wrapping
                Height = Dim.Fill(2) // <-- ADDED: Allow wrapping
            };

            var okButton = new Button("_OK")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(dialog) - 3, // <-- FIXED: Adjusted Y position
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };

            okButton.Clicked += () => Application.RequestStop(); // Stop the dialog

            dialog.Add(msgLabel, okButton);
            Application.Run(dialog);
        }

        /// <summary>
        /// Shows a custom, themed confirmation message.
        /// </summary>
        public static void ShowMessage(string title, string message)
        {
            var dialog = new Dialog(title, 60, 15) // <-- FIXED: Height increased from 10 to 15
            {
                ColorScheme = Colors.DialogScheme
            };

            var msgLabel = new Label(message)
            {
                X = 2,
                Y = 2,
                TextAlignment = TextAlignment.Centered,
                Width = Dim.Fill(2),  // <-- ADDED: Allow wrapping
                Height = Dim.Fill(2) // <-- ADDED: Allow wrapping
            };

            var okButton = new Button("_OK")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(dialog) - 3, // <-- FIXED: Adjusted Y position
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };

            okButton.Clicked += () => Application.RequestStop();

            dialog.Add(msgLabel, okButton);
            Application.Run(dialog);
        }

        /// <summary>
        /// Shows a custom, themed Yes/No question.
        /// </summary>
        /// <returns>True if Yes, False if No</returns>
        public static bool ShowQuery(string title, string message)
        {
            bool wasYes = false;
            var dialog = new Dialog(title, 60, 10)
            {
                ColorScheme = Colors.DialogScheme
            };

            var msgLabel = new Label(message) { X = 2, Y = 2, TextAlignment = TextAlignment.Centered };

            var yesButton = new Button("_Yes") // 'Y' is the hotkey
            {
                X = Pos.Center() - 10,
                Y = 6, // <-- FIXED: Was Pos.Bottom(dialog) - 5
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            yesButton.Clicked += () => {
                wasYes = true;
                Application.RequestStop();
            };

            var noButton = new Button("_No") // 'N' is the hotkey
            {
                X = Pos.Center() + 5,
                Y = 6, // <-- FIXED: Was Pos.Bottom(dialog) - 5
                ColorScheme = Colors.ButtonScheme
            };
            noButton.Clicked += () => {
                wasYes = false;
                Application.RequestStop();
            };

            dialog.Add(msgLabel, yesButton, noButton);
            Application.Run(dialog);
            return wasYes;
        }

        /// <summary>
        /// Helper to run a modal window (like a popup form).
        /// </summary>
        public static void OpenModal(Window window)
        {
            Application.Run(window);
        }
    }
}