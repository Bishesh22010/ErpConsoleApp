using Terminal.Gui;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations; // For [Key]
using System; // For DateTime

namespace ErpConsoleApp;

// --- STEP 1: DEFINE YOUR DATA MODELS ---

/// <summary>
/// This class represents a Party (supplier/customer) in our database
/// </summary>
public class Party
{
    [Key] // This tells EF Core this is the Primary Key
    public int PartyId { get; set; }

    [Required] // This means the column cannot be null
    [StringLength(100)]
    public string Name { get; set; }

    // We can add more fields later, like Address, Phone, etc.
}

/// <summary>
/// This class represents a single Purchase Slip
/// </summary>
public class PurchaseSlip
{
    [Key]
    public int PurchaseSlipId { get; set; }
    public DateTime Date { get; set; }

    [StringLength(100)]
    public string ItemName { get; set; }
    public decimal Amount { get; set; } // Use decimal for money

    // This sets up the "Foreign Key" relationship
    public int PartyId { get; set; }
    public Party Party { get; set; }
}


// --- STEP 2: DEFINE YOUR DATABASE CONTEXT ---
// This is the "brain" that connects our classes to the database

public class AppDbContext : DbContext
{
    // These DbSets become tables in our database
    public DbSet<Party> Parties { get; set; }
    public DbSet<PurchaseSlip> PurchaseSlips { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // This tells EF Core to create a SQLite database file
        // named "erp.db" in the same folder as the .exe
        options.UseSqlite("Data Source=erp.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add some seed data to start with
        modelBuilder.Entity<Party>().HasData(
            new Party { PartyId = 1, Name = "XYZ Party" },
            new Party { PartyId = 2, Name = "Main Supplier Inc." },
            new Party { PartyId = 3, Name = "Local Hardware" }
        );
    }
}


// --- STEP 3: UPDATE THE MAIN PROGRAM ---

class Program
{
    static int Main(string[] args)
    {
        // --- NEW: Initialize Database ---
        // This will create the erp.db file and tables if they don't exist
        try
        {
            using (var db = new AppDbContext())
            {
                // This applies any pending migrations (creates the database)
                db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            // If this fails, we can't run the app.
            Console.WriteLine($"Database initialization failed: {ex.Message}");
            return -1; // Exit with an error
        }

        // --- End of new code ---

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

    public static void OpenModal(Window window)
    {
        Application.Run(window);
    }
}

// ... LoginWindow class remains exactly the same ...
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

        quitButton.Clicked += () => {
            Application.RequestStop();
        };

        Add(pinLabel, pinField, loginButton, quitButton);
    }
}


// ... MenuWindow class remains exactly the same ...
class MenuWindow : Window
{
    private ListView moduleList;
    private FrameView rightPane;

    public MenuWindow() : base("Main Menu")
    {
        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var leftPane = new FrameView("Modules")
        {
            X = 0,
            Y = 0,
            Width = 30,
            Height = Dim.Fill()
        };

        var modules = new List<string>() { "Inventory", "Salary", "Stop" };

        moduleList = new ListView(modules)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill(),
            AllowsMarking = false,
            CanFocus = true
        };

        moduleList.SelectedItemChanged += OnModuleSelected;

        leftPane.Add(moduleList);

        rightPane = new FrameView("Options")
        {
            X = 30,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var welcomeLabel = new Label("Please select a module from the left.")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        rightPane.Add(welcomeLabel);

        Add(leftPane, rightPane);
        moduleList.SetFocus();
    }

    private void OnModuleSelected(ListViewItemEventArgs args)
    {
        rightPane.RemoveAll();

        string selectedModule = args.Value.ToString();
        rightPane.Title = $"{selectedModule} Options";

        if (selectedModule == "Inventory")
        {
            var inventoryOptionsList = new List<string>
            {
                "Purchase",
                "Payment",
                "Monthly Report",
                "PartyWise Report",
                "Item Wise Report",
                "Add & Delete Party",
                "Item Add and Delete",
                "Balance Sheet"
            };

            var inventoryOptions = new ListView(inventoryOptionsList)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill(),
                AllowsMarking = false,
                CanFocus = true
            };

            inventoryOptions.SelectedItemChanged += (e) => {
                string selectedOption = e.Value.ToString();

                if (selectedOption == "Purchase")
                {
                    Program.OpenModal(new PurchaseWindow());
                }
                else
                {
                    MessageBox.Query("Inventory", $"{selectedOption} screen will open here.", "OK");
                }
            };

            rightPane.Add(inventoryOptions);
            inventoryOptions.SetFocus();
        }
        else if (selectedModule == "Salary")
        {
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
            int selectedButton = MessageBox.Query("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (selectedButton == 0)
            {
                Program.ShowLoginPage();
            }
            else
            {
                rightPane.RemoveAll();
                var welcomeLabel = new Label("Please select a module from the left.")
                { X = Pos.Center(), Y = Pos.Center() };
                rightPane.Add(welcomeLabel);
                moduleList.SelectedItem = 0;
                moduleList.SetFocus();
            }
        }

        rightPane.LayoutSubviews();
    }
}

// --- STEP 4: UPDATE THE PURCHASE WINDOW ---

class PurchaseWindow : Window
{
    private DateField dateField;
    private ComboBox partyCombo;
    private TextField itemField;
    private TextField amountField;

    // --- NEW: Add a DbContext instance ---
    private AppDbContext db;

    public PurchaseWindow() : base("New Purchase Slip")
    {
        X = Pos.Center();
        Y = Pos.Center() - 5;
        Width = 60;
        Height = 16;
        Modal = true;

        // --- NEW: Initialize the DbContext ---
        db = new AppDbContext();

        var dateLabel = new Label("Date:") { X = 2, Y = 2 };
        dateField = new DateField(System.DateTime.Now)
        {
            X = Pos.Right(dateLabel) + 8,
            Y = 2,
            Width = 20,
            IsShortFormat = true
        };

        var partyLabel = new Label("Party Name:") { X = 2, Y = 4 };
        partyCombo = new ComboBox()
        {
            X = Pos.Right(partyLabel) + 1,
            Y = 4,
            Width = 40,
            Height = 4
        };

        // --- NEW: Load parties from database ---
        LoadParties();

        var itemLabel = new Label("Item Name:") { X = 2, Y = 6 };
        itemField = new TextField("")
        {
            X = Pos.Right(itemLabel) + 2,
            Y = 6,
            Width = 40
        };

        var amountLabel = new Label("Amount:") { X = 2, Y = 8 };
        amountField = new TextField("")
        {
            X = Pos.Right(amountLabel) + 5,
            Y = 8,
            Width = 20
        };

        var generateButton = new Button("Generate Slip")
        {
            X = Pos.Center() - 15,
            Y = 12,
            IsDefault = true
        };
        generateButton.Clicked += OnGenerateSlip;

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(generateButton) + 2,
            Y = 12
        };
        cancelButton.Clicked += () => {
            db.Dispose(); // --- NEW: Dispose the context
            Application.RequestStop();
        };

        Add(dateLabel, dateField, partyLabel, partyCombo, itemLabel, itemField, amountLabel, amountField, generateButton, cancelButton);
        dateField.SetFocus();
    }

    /// <summary>
    /// Helper method to load party names from the DB
    /// </summary>
    private void LoadParties()
    {
        var partyNames = db.Parties
            .Select(p => p.Name)
            .OrderBy(name => name)
            .ToList();
        partyCombo.SetSource(partyNames);
    }

    private void OnGenerateSlip()
    {
        string partyName = partyCombo.Text.ToString();
        string itemName = itemField.Text.ToString();

        // 1. Validate data
        if (string.IsNullOrWhiteSpace(partyName) || string.IsNullOrWhiteSpace(itemName) || !decimal.TryParse(amountField.Text.ToString(), out decimal amount))
        {
            MessageBox.ErrorQuery("Error", "Party, Item, and a valid Amount are required.", "OK");
            return;
        }

        try
        {
            // 2. Find or Create the Party
            // Check if party already exists (case-insensitive)
            var party = db.Parties.FirstOrDefault(p => p.Name.ToLower() == partyName.ToLower());

            if (party == null) // Party doesn't exist
            {
                party = new Party { Name = partyName };
                db.Parties.Add(party);
                // We must save here so the 'party' object gets a valid PartyId
                db.SaveChanges();
            }

            // 3. Create the new Purchase Slip
            var newSlip = new PurchaseSlip
            {
                Date = dateField.Date,
                ItemName = itemName,
                Amount = amount,
                PartyId = party.PartyId // Link the slip to the party
            };

            db.PurchaseSlips.Add(newSlip);
            db.SaveChanges(); // Save the new slip to the database

            // 4. Show the "Slip"
            string slipMessage = $"--- Purchase Slip ---\n\n" +
                                 $"Date: {newSlip.Date.ToShortDateString()}\n" +
                                 $"Party: {party.Name}\n" +
                                 $"Item: {newSlip.ItemName}\n" +
                                 $"Amount: {newSlip.Amount:C}\n\n" + // :C formats as currency
                                 $"Slip generated and saved to database.";

            MessageBox.Query("Success", slipMessage, "OK");

            // 5. Clear the form and reload parties (in case a new one was added)
            itemField.Text = "";
            amountField.Text = "";
            LoadParties(); // Reload list
            partyCombo.Text = partyName; // Keep the party name
            itemField.SetFocus();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Database Error", $"Could not save slip: {ex.Message}", "OK");
        }
    }
}


// ... AppMenuBar class remains exactly the same ...
class AppMenuBar : MenuBar
{
    public AppMenuBar()
    {
        Menus = new MenuBarItem[] {
            new MenuBarItem("_File", new MenuItem[] {
                new MenuItem("_Logout", "", () => Program.ShowLoginPage()),
                new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
            }),
            new MenuBarItem("_Inventory", new MenuItem[] {
                new MenuItem("_Purchase", "", () => Program.OpenModal(new PurchaseWindow())),
                new MenuItem("_Payment", "", () => MessageBox.Query("Inventory", "Payment screen opens here.", "OK")),
                new MenuItem("_Item Add/Delete", "", () => {
                    MessageBox.Query("Inventory", "Item Add/Delete window opens here.", "OK");
                }),
                new MenuItem("_Party Add/Delete", "", () => MessageBox.Query("Inventory", "Party Add/Delete window opens here.", "OK"))
            }),
            new MenuBarItem("_Salary", new MenuItem[] {
                new MenuItem("_Run Payroll", "", () => MessageBox.Query("Salary", "Run Payroll screen opens here.", "OK")),
                new MenuItem("_View Employees", "", () => MessageBox.Query("Salary", "View Employees screen opens here.", "OK"))
            })
        };
    }
}