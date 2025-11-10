using Terminal.Gui;
using System.Collections.Generic; // We need this for List<string>

namespace ErpConsoleApp;

class Program
{
    static int Main(string[] args)
    {
        Application.Init();
        Application.Top.Add(new LoginWindow());
        Application.Run();
        Application.Shutdown();
        return 0;
    }

    public static void ShowMenuPage()
    {
        Application.Top.RemoveAll();
        Application.Top.Add(new AppMenuBar());
        Application.Top.Add(new MenuWindow());
    }

    public static void ShowLoginPage()
    {
        Application.Top.RemoveAll();
        Application.Top.Add(new LoginWindow());
    }
}

/// <summary>
/// The Login screen (Same as before)
/// </summary>
class LoginWindow : Window
{
    private TextField pinField;

    public LoginWindow() : base("Login (Press Ctrl+Q to quit)")
    {
        X = Pos.Center();
        Y = Pos.Center() - 2;
        Width = 40;
        Height = 10;
        Modal = true;

        var pinLabel = new Label("Enter 4-Digit PIN:") { X = 2, Y = 2 };

        pinField = new TextField("")
        {
            X = Pos.Right(pinLabel) + 1,
            Y = 2,
            Width = 10,
            Secret = true
        };
        pinField.SetFocus();

        var loginButton = new Button("Login")
        {
            X = Pos.Center() - 10,
            Y = 6,
            IsDefault = true,
        };

        // We use += to add the event handler
        loginButton.Clicked += () => {
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
        };

        // We use += to add the event handler
        quitButton.Clicked += () => {
            Application.RequestStop();
        };

        Add(pinLabel, pinField, loginButton, quitButton);
    }
}

/// <summary>
/// The new main menu window with a two-pane layout
/// </summary>
class MenuWindow : Window
{
    private ListView moduleList;
    private FrameView rightPane; // This pane will show sub-options

    public MenuWindow() : base("Main Menu")
    {
        X = 0;
        Y = 1; // Start below the MenuBar
        Width = Dim.Fill();
        Height = Dim.Fill();

        // --- Left Module List ---
        var leftPane = new FrameView("Modules")
        {
            X = 0,
            Y = 0,
            Width = 30, // 30 characters wide
            Height = Dim.Fill()
        };

        var modules = new List<string>() { "Inventory", "Salary", "Stop" };

        moduleList = new ListView(modules)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill() - 1, // Fill the frame
            Height = Dim.Fill(),
            AllowsMarking = false,
            CanFocus = true
        };

        // This event triggers when you select a module
        moduleList.SelectedItemChanged += OnModuleSelected;

        leftPane.Add(moduleList);

        // --- Right Pane for Sub-Options ---
        rightPane = new FrameView("Options")
        {
            X = 30, // Position it next to the left pane
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Add a welcome message to the right pane
        var welcomeLabel = new Label("Please select a module from the left.")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        rightPane.Add(welcomeLabel);


        // Add the panes to the main window
        Add(leftPane, rightPane);

        // Set initial focus on the module list
        moduleList.SetFocus();
    }

    /// <summary>
    /// This method is called when a module is selected
    /// </summary>
    private void OnModuleSelected(ListViewItemEventArgs args)
    {
        // Clear all old controls from the right pane
        rightPane.RemoveAll();

        string selectedModule = args.Value.ToString();

        // Set the title of the right pane
        rightPane.Title = $"{selectedModule} Options";

        if (selectedModule == "Inventory")
        {
            // --- INVENTORY SUB-MENU ---
            var inventoryOptions = new ListView(new List<string> { "View Stock", "Add New Item", "Process Sale", "Receive Stock" })
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill()
            };

            // We can add events to these sub-options later
            inventoryOptions.SelectedItemChanged += (e) => {
                if (e.Value.ToString() == "Add New Item")
                {
                    // This is where we'll show the "Add New Item" window
                    // For now, just a message
                    MessageBox.Query("Inventory", "Add New Item screen will open here.", "OK");
                }
            };

            rightPane.Add(inventoryOptions);
        }
        else if (selectedModule == "Salary")
        {
            // --- SALARY SUB-MENU ---
            var salaryOptions = new ListView(new List<string> { "Run Payroll", "View Employees", "Add New Employee", "View Payslips" })
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill()
            };
            rightPane.Add(salaryOptions);
        }
        else if (selectedModule == "Stop")
        {
            // --- STOP (Logout Confirmation) ---

            // Show a Yes/No confirmation box
            // MessageBox.Query returns 0 if the first button ("Yes") is clicked, 1 for "No".
            int selectedButton = MessageBox.Query(
                "Logout",
                "Are you sure you want to logout?",
                "Yes", "No");

            if (selectedButton == 0) // User clicked "Yes"
            {
                Program.ShowLoginPage();
            }
            else
            {
                // User clicked "No", let's clear the right pane
                // and select the first item in the list so they can
                // continue working.
                rightPane.RemoveAll();
                var welcomeLabel = new Label("Please select a module from the left.")
                {
                    X = Pos.Center(),
                    Y = Pos.Center()
                };
                rightPane.Add(welcomeLabel);

                // Reselect the first item
                moduleList.SelectedItem = 0;
                moduleList.SetFocus();
            }
        }

        // Refresh the right pane to show the new controls
        rightPane.LayoutSubviews();
    }
}

/// <summary>
/// This is the top-level Menu Bar, now with actions
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
                new MenuItem("_View Stock", "", () => MessageBox.Query("Inventory", "View Stock screen opens here.", "OK")),
                new MenuItem("_Add New Item", "", () => {
                    // This is how we'll open new "pop-up" windows
                    // We'll build this 'AddNewItemWindow' class next
                    MessageBox.Query("Inventory", "Add New Item window opens here.", "OK");
                })
            }),
            new MenuBarItem("_Salary", new MenuItem[] {
                new MenuItem("_Run Payroll", "", () => MessageBox.Query("Salary", "Run Payroll screen opens here.", "OK")),
                new MenuItem("_View Employees", "", () => MessageBox.Query("Salary", "View Employees screen opens here.", "OK"))
            })
        };
    }
}