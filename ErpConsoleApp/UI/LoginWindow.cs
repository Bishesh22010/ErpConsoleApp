using System;
using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// The Login screen
    /// </summary>
    public class LoginWindow : Window
    {
        private TextField pinField;

        public LoginWindow() : base("Login (Ctrl+Q to quit)")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center();
            Y = Pos.Center() - 2; // Move it up a bit
            Width = 40;
            Height = 10;
            Modal = true;

            var pinLabel = new Label("Enter 4-Digit PIN:")
            {
                X = 2,
                Y = 2
            };

            pinField = new TextField("")
            {
                X = Pos.Right(pinLabel) + 1,
                Y = 2,
                Width = 10,
                Secret = true, // This makes it show '*' for the password
                ColorScheme = Colors.TextScheme
            };
            pinField.SetFocus();

            var loginButton = new Button("_Login")
            {
                X = Pos.Center() - 10,
                Y = 6,
                IsDefault = true, // Pressing Enter will click this button
                ColorScheme = Colors.ButtonScheme
            };

            loginButton.Clicked += () => {
                // --- THIS IS OUR LOGIN LOGIC ---
                if (pinField.Text.ToString() == "1234")
                {
                    // Correct PIN. Call the static method to show the menu.
                    Program.ShowMenuPage();
                }
                else
                {
                    // Incorrect PIN
                    Program.ShowError("Login Failed", "Incorrect PIN. Please try again.");
                    pinField.Text = ""; // Clear the text field
                }
            };

            var quitButton = new Button("_Quit")
            {
                X = Pos.Center() + 2,
                Y = 6,
                ColorScheme = Colors.ButtonScheme
            };

            quitButton.Clicked += () => {
                Application.RequestStop(); // Stop the whole app
            };

            Add(pinLabel, pinField, loginButton, quitButton);
        }
    }
}