using System;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class LoginWindow : Window
    {
        private TextField pinField;
        private TextField confirmPinField; // Only for setup
        private Button actionButton;
        private bool isSetupMode = false;

        public LoginWindow() : base("Welcome to ERP System")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center(); Y = Pos.Center(); Width = 40; Height = 12;
            Modal = true;

            // Check if this is the first run
            CheckFirstRun();

            // If setup mode, we need extra space
            if (isSetupMode)
            {
                Title = "First Run Setup: Create PIN";
                Height = 14;
            }
            else
            {
                Title = "Login (Ctrl+Q to quit)";
            }

            var pinLabel = new Label(isSetupMode ? "Create New PIN:" : "Enter 4-Digit PIN:") { X = 2, Y = 2 };
            pinField = new TextField("") { X = Pos.Right(pinLabel) + 1, Y = 2, Width = 10, Secret = true, ColorScheme = Colors.TextScheme };

            Add(pinLabel, pinField);

            if (isSetupMode)
            {
                var confirmLabel = new Label("Confirm PIN:") { X = 2, Y = 4 };
                confirmPinField = new TextField("") { X = Pos.Right(pinLabel) + 1, Y = 4, Width = 10, Secret = true, ColorScheme = Colors.TextScheme };
                Add(confirmLabel, confirmPinField);
            }

            actionButton = new Button(isSetupMode ? "_Set PIN & Login" : "_Login")
            {
                X = Pos.Center() - 10,
                Y = isSetupMode ? 8 : 6,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            actionButton.Clicked += OnAction;

            var quitButton = new Button("_Quit")
            {
                X = Pos.Center() + 5,
                Y = isSetupMode ? 8 : 6,
                ColorScheme = Colors.ButtonScheme
            };
            quitButton.Clicked += () => Application.RequestStop();

            Add(actionButton, quitButton);
            pinField.SetFocus();
        }

        private void CheckFirstRun()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();
                    // If no PIN exists in settings, it's first run
                    var pinSetting = db.Settings.Find("LoginPin");
                    isSetupMode = (pinSetting == null);
                }
            }
            catch (Exception)
            {
                // If DB fails, assume normal login or show error later
                isSetupMode = false;
            }
        }

        private void OnAction()
        {
            if (isSetupMode)
            {
                HandleSetup();
            }
            else
            {
                HandleLogin();
            }
        }

        private void HandleSetup()
        {
            string pin = pinField.Text.ToString();
            string confirm = confirmPinField.Text.ToString();

            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            {
                Program.ShowError("Error", "PIN must be at least 4 digits."); return;
            }
            if (pin != confirm)
            {
                Program.ShowError("Error", "PINs do not match."); return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Settings.Add(new AppSetting { Key = "LoginPin", Value = pin });
                    db.SaveChanges();
                }
                Program.ShowMessage("Success", "PIN created successfully!\nLogging in...");
                Program.ShowMenuPage();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void HandleLogin()
        {
            string inputPin = pinField.Text.ToString();
            string dbPin = "";

            try
            {
                using (var db = new AppDbContext())
                {
                    var setting = db.Settings.Find("LoginPin");
                    if (setting != null) dbPin = setting.Value;
                }

                if (inputPin == dbPin)
                {
                    Program.ShowMenuPage();
                }
                else
                {
                    Program.ShowError("Login Failed", "Incorrect PIN.");
                    pinField.Text = "";
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}