using System;
using System.Collections.Generic;
using Terminal.Gui;
using ErpConsoleApp.Database; // Ensure we have access if needed

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// The main application window after logging in.
    /// </summary>
    public class MenuWindow : Window
    {
        private ListView moduleList;
        // private FrameView optionsFrame; // REMOVED: Duplicate logic
        private FrameView rightPane;       // This is the main right-side container
        private ListView optionsList;

        // Lists of options for the right-hand pane
        private List<string> inventoryOptions = new List<string> {
            "Add & Delete Party",
            "Item Add and Delete",
            "Purchase",
            "Payment",
            "Monthly Report",
            "PartyWise Report",
            "Item Wise Report",
            "Balance Sheet"
        };

        // --- UPDATED SALARY OPTIONS ---
        private List<string> salaryOptions = new List<string> {
            "Manage Employee",
            "Salary",
            "Voucher",
            "Reports",
            "Settings" // Settings is in the list, but handled specially
        };

        public MenuWindow() : base("Main Menu")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0;
            Y = 1; // Start on row 1, because the MenuBar is on row 0
            Width = Dim.Fill();
            Height = Dim.Fill();

            // --- 1. Initialize Left Pane ---
            var leftPane = new FrameView("Modules")
            {
                X = 0,
                Y = 0,
                Width = 25,
                Height = Dim.Fill(),
                ColorScheme = Colors.WindowScheme
            };

            // Note: 'Settings' is included here in the main module list based on your screenshot
            moduleList = new ListView(new List<string> { "Inventory", "Salary", "Settings", "Stop" })
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            leftPane.Add(moduleList);

            // --- 2. Initialize Right Pane (CRITICAL: Do this before setting events) ---
            // FIXED: Assign to 'rightPane', NOT 'optionsFrame'
            rightPane = new FrameView("Options")
            {
                X = 25,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.WindowScheme
            };

            optionsList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };
            // Event handler for when an option inside the right pane is selected
            optionsList.OpenSelectedItem += OnOptionSelected;

            // Initially add the options list to the right pane (default state)
            rightPane.Add(optionsList);

            // Add panes to the window
            Add(leftPane, rightPane);

            // --- 3. Subscribe to Events & Set Initial State ---
            // NOW it is safe to subscribe, because rightPane is not null.
            moduleList.SelectedItemChanged += OnModuleSelected;

            // Trigger the initial selection manually to load the first item (Inventory)
            moduleList.SelectedItem = 0;
            moduleList.SetFocus();
        }

        private void OnModuleSelected(ListViewItemEventArgs args)
        {
            // Safety check
            if (rightPane == null) return;

            // Clear whatever is currently in the right pane
            rightPane.RemoveAll();
            
            if (args.Value == null) return;
            string selectedModule = args.Value.ToString();
            
            // Update the title of the right pane
            rightPane.Title = selectedModule; 

            if (selectedModule == "Inventory")
            {
                rightPane.Title = "Inventory Options";
                optionsList.SetSource(inventoryOptions);
                rightPane.Add(optionsList);
            }
            else if (selectedModule == "Salary")
            {
                rightPane.Title = "Salary Options";
                optionsList.SetSource(salaryOptions);
                rightPane.Add(optionsList);
            }
            else if (selectedModule == "Settings")
            {
                // --- EMBED SETTINGS DIRECTLY ---
                rightPane.Title = "System Settings";
                rightPane.Add(new SettingsView());
            }
            else if (selectedModule == "Stop")
            {
                // Ask for logout confirmation
                if (Program.ShowQuery("Logout", "Are you sure you want to logout?"))
                {
                    Program.ShowLoginPage();
                }
                else
                {
                    // Cancelled, go back to top
                    // This might trigger this event again, so be careful.
                    // Setting it to 0 (Inventory) is safe.
                    moduleList.SelectedItem = 0;
                    // Ensure we re-render the Inventory view since we just cleared rightPane
                    rightPane.Title = "Inventory Options";
                    optionsList.SetSource(inventoryOptions);
                    rightPane.Add(optionsList);
                }
            }
        }

        private void OnOptionSelected(ListViewItemEventArgs args)
        {
            if (args.Value == null) return;
            string selectedOption = args.Value.ToString();

            try 
            {
                if (selectedOption == "Purchase") Program.OpenModal(new PurchaseWindow());
                else if (selectedOption == "Add & Delete Party") Program.OpenModal(new ManagePartiesWindow());
                else if (selectedOption == "Payment") Program.OpenModal(new PaymentWindow());
                else if (selectedOption == "Item Add and Delete") Program.OpenModal(new ManageItemsWindow());
                else if (selectedOption == "Monthly Report") Program.OpenModal(new MonthlyReportWindow());
                else if (selectedOption == "PartyWise Report") Program.OpenModal(new PartyWiseReportWindow());
                
                else if (selectedOption == "Manage Employee") Program.OpenModal(new ManageEmployeeWindow());
                else if (selectedOption == "Salary") Program.OpenModal(new SalaryWindow());
                else if (selectedOption == "Voucher") Program.OpenModal(new VoucherWindow());
                
                else if (selectedOption == "Reports") Program.OpenModal(new ReportsWindow());
                
                // If "Settings" is clicked from the Salary sub-menu (as per your list), 
                // we can decide what to do. Since Settings is now a main module, 
                // clicking it here could just switch the main module selection, 
                // or open the modal wrapper if you prefer.
                else if (selectedOption == "Settings") 
                {
                    // Option A: Switch the main left menu to "Settings"
                    // moduleList.SelectedItem = 2; // Assuming Settings is at index 2
                    
                    // Option B: Just show the view right here (simpler)
                    rightPane.RemoveAll();
                    rightPane.Title = "System Settings";
                    rightPane.Add(new SettingsView());
                }
                else
                {
                    Program.ShowMessage("Not Implemented", $"The action '{selectedOption}' is not yet implemented.");
                }
            }
            catch (Exception ex)
            {
                Program.ShowError("Error", $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}