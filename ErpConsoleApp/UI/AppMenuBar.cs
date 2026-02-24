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
                new MenuBarItem("_File", new MenuItem[] {
                    new MenuItem("_Logout", "", () => Program.ShowLoginPage()),
                    new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
                }),
                new MenuBarItem("_Inventory", new MenuItem[] {
                    new MenuItem("_Purchase", "", () => Program.OpenModal(new PurchaseWindow())),
                    new MenuItem("_Payment", "", () => Program.OpenModal(new PaymentWindow())),
                    new MenuItem("_Item Add and Delete", "",() => Program.OpenModal(new ManageItemsWindow())),
                    new MenuItem("_Monthly Report", "", () => Program.OpenModal(new MonthlyReportWindow())),
                    new MenuItem("_PartyWise Report", "", () => Program.OpenModal(new PartyWiseReportWindow())),
                    new MenuItem("_Item Wise Report", "", null),
                    new MenuItem("_Add & Delete Party", "", null),
                    
                    new MenuItem("_Balance Sheet", "", null)
                }),
                new MenuBarItem("_Salary  (Press Alt", new MenuItem[] {
                    new MenuItem("_Manage Employee", "",() => Program.OpenModal(new ManageEmployeeWindow())),
                    new MenuItem("_Salary", "",() => Program.OpenModal(new SalaryWindow())),
                    new MenuItem("_Voucher", "",() => Program.OpenModal(new VoucherWindow())),
                    new MenuItem("_Reports", "",() => Program.OpenModal(new ReportsWindow())),
                    new MenuItem("_Settings", "", null)
                })
            };
        }
    }
}