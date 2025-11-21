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
        private FrameView optionsFrame;
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
            "Settings"
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

            moduleList = new ListView(new List<string> { "Inventory", "Salary", "Stop" })
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
            optionsFrame = new FrameView("Options")
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

            optionsFrame.Add(optionsList);

            Add(leftPane, optionsFrame);

            // Set initial state
            moduleList.SetSource(new List<string> { "Inventory", "Salary", "Stop" });
            moduleList.SelectedItem = 0;
            moduleList.SetFocus();
        }

        /// <summary>
        /// Called when user selects "Inventory", "Salary", or "Stop".
        /// </summary>
        private void OnModuleSelected(ListViewItemEventArgs args)
        {
            string selectedModule = args.Value.ToString();

            if (selectedModule == "Inventory")
            {
                optionsFrame.Title = "Inventory Options";
                optionsList.SetSource(inventoryOptions);
            }
            else if (selectedModule == "Salary")
            {
                optionsFrame.Title = "Salary Options";
                optionsList.SetSource(salaryOptions);
            }
            else if (selectedModule == "Stop")
            {
                // Ask for logout confirmation
                if (Program.ShowQuery("Logout", "Are you sure you want to logout?"))
                { // Reset selection if they click "No"
                    optionsFrame.Title = "Options";
                    optionsList.SetSource(new List<string>());
                    moduleList.SelectedItem = 0; // Go back to "Inventory"
                    
                }
                else
                {
                    Program.ShowLoginPage();
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
            else if(selectedOption == "Payment")
            {
                Program.OpenModal(new PaymentWindow());
            }
            else if(selectedOption == "Item Add and Delete")
            {
                Program.OpenModal(new ManageItemsWindow());
            }
            else if(selectedOption == "Monthly Report")
            {
                Program.OpenModal(new MonthlyReportWindow());
            }
            else if(selectedOption == "PartyWise Report")
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
            else
            {
                Program.ShowMessage("Not Implemented", $"The action '{selectedOption}' is not yet implemented.");
            }
        }
    }
}