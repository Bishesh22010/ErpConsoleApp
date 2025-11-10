using Terminal.Gui;

namespace ErpConsoleApp;

class Program
{
    static int Main(string[] args)
    {
        Application.Init();

        // We start by adding the Login Window
        Application.Top.Add(new LoginWindow());

        // Run the application
        Application.Run();
        Application.Shutdown();
        return 0;
    }

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
    }

    /// <summary>
    /// Navigation method to go back to the login screen.
    /// </summary>
    public static void ShowLoginPage()
    {
        Application.Top.RemoveAll();
        Application.Top.Add(new LoginWindow());
    }
}

/// <summary>
/// The Login screen
/// </summary>
class LoginWindow : Window
{
    private TextField pinField;

    public LoginWindow() : base("Login (Press Ctrl+Q to quit)")
    {
        // Center the login box on the screen
        X = Pos.Center();
        Y = Pos.Center() - 2; // Move it up a bit
        Width = 40;
        Height = 10;

        // 'Modal' means the user can't click anything else
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
            Secret = true // This makes it show '*' for the password
        };
        // Set focus to the PIN field
        pinField.SetFocus();

        var loginButton = new Button("Login")
        {
            X = Pos.Center() - 10,
            Y = 6,
            IsDefault = true, // Pressing Enter will click this button

            // ✔️ CORRECT SYNTAX (using = inside the initializer)
            /* We will move this outside
            Clicked = () => {
                // --- THIS IS OUR LOGIN LOGIC ---
                if (pinField.Text.ToString() == "1234")
                {
                    Program.ShowMenuPage();
                }
                else
                {
                    MessageBox.ErrorQuery("Login Failed", "Incorrect PIN. Please try again.", "OK");
                    pinField.Text = ""; 
                }
            }
            */
        };

        // ✔️ NEW FIX: Assigning the event using +=
        // This is the standard way and will resolve the error.
        loginButton.Clicked += () => {
            // --- THIS IS OUR LOGIN LOGIC ---
            if (pinField.Text.ToString() == "1234")
            {
                Program.ShowMenuPage();
            }
            else
            {
                MessageBox.ErrorQuery("Login Failed", "Incorrect PIN. Please try again.", "OK");
                pinField.Text = "";
            }
        };

        var quitButton = new Button("Quit")
        {
            X = Pos.Center() + 2,
            Y = 6,

            // ✔️ CORRECT SYNTAX (using = inside the initializer)
            /* We will move this outside
            Clicked = () => {
                Application.RequestStop(); // Stop the whole app
            }
            */
        };

        // ✔️ NEW FIX: Assigning the event using +=
        quitButton.Clicked += () => {
            Application.RequestStop(); // Stop the whole app
        };

        Add(pinLabel, pinField, loginButton, quitButton);
    }
}

/// <summary>
/// The main application window after logging in.
/// </summary>
class MenuWindow : Window
{
    public MenuWindow() : base("Main Menu")
    {
        X = 0;
        Y = 1; // Start on row 1, because the MenuBar is on row 0
        Width = Dim.Fill();
        Height = Dim.Fill();

        var welcomeLabel = new Label("Login Successful! Welcome to the Main Menu.")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };

        Add(welcomeLabel);
    }
}

/// <summary>
/// This is the top-level Menu Bar, like in your photo.
/// </summary>
class AppMenuBar : MenuBar
{
    public AppMenuBar()
    {
        // Define the menu structure
        Menus = new MenuBarItem[] {
            new MenuBarItem("_File", new MenuItem[] {
                new MenuItem("_Logout", "", () => Program.ShowLoginPage()),
                new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
            }),
            new MenuBarItem("_Inventory", new MenuItem[] {
                new MenuItem("_View Stock", "", null),
                new MenuItem("_Add New Item", "", null)
            }),
            new MenuBarItem("_Salary", new MenuItem[] {
                new MenuItem("_Run Payroll", "", null),
                new MenuItem("_View Employees", "", null)
            })
        };
    }
}