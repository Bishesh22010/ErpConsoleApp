using System;
using System.Linq; // Needed for DB query
using Terminal.Gui;
using ErpConsoleApp.Database; // Needed for DB context

namespace ErpConsoleApp.UI
{
    public class LoginWindow : Window
    {
        private TextField pinField;

        public LoginWindow() : base("Login (Ctrl+Q to quit)")
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center();
            Y = Pos.Center() - 2;
            Width = 40;
            Height = 10;
            Modal = true;

            var pinLabel = new Label("Enter Your PIN:") { X = 2, Y = 2 };

            pinField = new TextField("")
            {
                X = Pos.Right(pinLabel) + 1,
                Y = 2,
                Width = 10,
                Secret = true,
                ColorScheme = Colors.TextScheme
            };
            pinField.SetFocus();

            var loginButton = new Button("_Login")
            {
                X = Pos.Center() - 10,
                Y = 6,
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };

            loginButton.Clicked += OnLogin;

            var quitButton = new Button("_Quit")
            {
                X = Pos.Center() + 2,
                Y = 6,
                ColorScheme = Colors.ButtonScheme
            };
            quitButton.Clicked += () => Application.RequestStop();

            Add(pinLabel, pinField, loginButton, quitButton);
        }

        private void OnLogin()
        {
            string inputPin = pinField.Text.ToString();
            string dbPin = "1234"; // Default fallback

            try
            {
                using (var db = new AppDbContext())
                {
                    // Create DB if not exists (handles fresh install scenario)
                    db.Database.EnsureCreated();

                    var setting = db.Settings.Find("LoginPin");
                    if (setting != null)
                    {
                        dbPin = setting.Value;
                    }
                    else
                    {
                        // If missing, seed it
                        db.Settings.Add(new Database.Models.AppSetting { Key = "LoginPin", Value = "1234" });
                        db.SaveChanges();
                    }
                }

                if (inputPin == dbPin)
                {
                    Program.ShowMenuPage();
                }
                else
                {
                    Program.ShowError("Login Failed", "Incorrect PIN. Please try again.");
                    pinField.Text = "";
                }
            }
            catch (Exception e)
            {
                Program.ShowError("DB Error", "Could not connect to database.\n" + e.Message);
            }
        }
    }
}