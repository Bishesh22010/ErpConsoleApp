using System;
using System.Collections.Generic;
using Terminal.Gui;

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
        };

        public MenuWindow() : base("Main Menu")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0;
            Y = 1; // Start on row 1, because the MenuBar is on row 0
            Width = Dim.Fill();
            Height = Dim.Fill();

            // --- Left Pane (Modules) ---
            var leftPane = new FrameView("Modules")
            {
                X = 0,
                Y = 0,
                Width = 25,
                Height = Dim.Fill(),
                ColorScheme = Colors.WindowScheme
            };

            moduleList = new ListView(new List<string> { "Inventory", "Salary", "Settings", "Stop" })
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TextScheme
            };

            // Event handler for when a module is selected
            moduleList.SelectedItemChanged += OnModuleSelected;

            leftPane.Add(moduleList);

            // --- Right Pane (Options) ---
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

            // Event handler for when an option is selected (e.g., "Purchase")
            optionsList.OpenSelectedItem += OnOptionSelected;

            // Initially add the options list to the right pane
            rightPane.Add(optionsList);

            Add(leftPane, rightPane);

            // Set initial state
            moduleList.SetSource(new List<string> { "Inventory", "Salary", "Settings", "Stop" });
            moduleList.SelectedItem = 0;
            moduleList.SetFocus();
        }

        /// <summary>
        /// Called when user selects "Inventory", "Salary", or "Stop".
        /// </summary>
        private void OnModuleSelected(ListViewItemEventArgs args)
        {
            // Clear whatever is currently in the right pane
            // This logic works now because rightPane is initialized!
            rightPane.RemoveAll();

            string selectedModule = args.Value.ToString();
            rightPane.Title = $"{selectedModule}";

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
                // --- NEW: EMBED SETTINGS DIRECTLY ---
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
                    // We need to restore the previous view if they cancel logout from "Stop"
                    // Default back to Inventory for simplicity
                    moduleList.SelectedItem = 0;
                }
            }
        }

        /// <summary>
        /// Called when user presses Enter on an option (e.g., "Purchase").
        /// </summary>
        private void OnOptionSelected(ListViewItemEventArgs args)
        {
            string selectedOption = args.Value.ToString();

            if (selectedOption == "Purchase")
            {
                // Open the Purchase window as a modal dialog
                Program.OpenModal(new PurchaseWindow());
            }
            // --- ADDED THIS BLOCK ---
            else if (selectedOption == "Add & Delete Party")
            {
                Program.OpenModal(new ManagePartiesWindow());
            }
            // --- END OF ADDED BLOCK ---
            else if (selectedOption == "Payment")
            {
                Program.OpenModal(new PaymentWindow());
            }
            else if (selectedOption == "Item Add and Delete")
            {
                Program.OpenModal(new ManageItemsWindow());
            }
            else if (selectedOption == "Monthly Report")
            {
                Program.OpenModal(new MonthlyReportWindow());
            }
            else if (selectedOption == "PartyWise Report")
            {
                Program.OpenModal(new PartyWiseReportWindow());
            }
            else if (selectedOption == "Manage Employee")
            {
                Program.OpenModal(new ManageEmployeeWindow());
            }
            else if (selectedOption == "Salary")
            {
                Program.OpenModal(new SalaryWindow());
            }
            else if (selectedOption == "Voucher")
            {
                Program.OpenModal(new VoucherWindow());
            }
            else if (selectedOption == "Reports")
            {
                Program.OpenModal(new ReportsWindow());
            }
            else
            {
                Program.ShowMessage("Not Implemented", $"The action '{selectedOption}' is not yet implemented.");
            }
        }
    }
}