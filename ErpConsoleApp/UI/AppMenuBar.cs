using System;
using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// This is the top-level Menu Bar.
    /// </summary>
    public class AppMenuBar : MenuBar
    {
        public AppMenuBar()
        {
            ColorScheme = Colors.MenuScheme;

            // Define the menu structure
            Menus = new MenuBarItem[] {
                new MenuBarItem("_File (F9)", new MenuItem[] {
                    new MenuItem("_Logout", "", () => Program.ShowLoginPage()),
                    new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
                }),
                new MenuBarItem("_Inventory", new MenuItem[] {
                    new MenuItem("_Purchase", "", () => Program.OpenModal(new PurchaseWindow())),
                    new MenuItem("_Payment", "", () => Program.OpenModal(new PaymentWindow())),
                    new MenuItem("_Item Add and Delete", "",() => Program.OpenModal(new PaymentWindow())),
                    new MenuItem("_Monthly Report", "", () => Program.OpenModal(new MonthlyReportWindow()),
                    new MenuItem("_PartyWise Report", "", null),
                    new MenuItem("_Item Wise Report", "", null),
                    new MenuItem("_Add & Delete Party", "", null),
                    
                    new MenuItem("_Balance Sheet", "", null)
                }),
                new MenuBarItem("_Salary", new MenuItem[] {
                    new MenuItem("_Run Payroll", "", null),
                    new MenuItem("_View Employees", "", null)
                })
            };
        }
    }
}