using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class VoucherWindow : Window
    {
        private ListView employeeList;
        private ListView voucherList;
        private List<Employee> allEmployees = new List<Employee>();
        private List<Voucher> employeeVouchers = new List<Voucher>();
        private Employee selectedEmployee = null;

        public VoucherWindow() : base("Voucher Management (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            KeyDown += (e) => { if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; } };

            // --- Left Pane: Employees ---
            var leftFrame = new FrameView("Select Employee")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(40),
                Height = Dim.Fill()
            };
            employeeList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            employeeList.SelectedItemChanged += OnEmployeeSelected;
            leftFrame.Add(employeeList);

            // --- Right Pane: Vouchers ---
            var rightFrame = new FrameView("Issued Vouchers")
            {
                X = Pos.Right(leftFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2)
            };

            // Header Row
            var header = new Label(string.Format("{0,-12} | {1,10} | {2}", "Date", "Amount", "Reason"))
            { X = 0, Y = 0, ColorScheme = Colors.MenuScheme };

            voucherList = new ListView() { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            rightFrame.Add(header, voucherList);

            // --- Bottom: Actions ---
            var btnGenerate = new Button("_Generate Voucher") { X = Pos.Percent(50), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnGenerate.Clicked += OnGenerate;

            var btnUpdate = new Button("_Update Voucher") { X = Pos.Right(btnGenerate) + 2, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnUpdate.Clicked += OnUpdate;

            var btnDelete = new Button("_Delete Voucher") { X = Pos.Right(btnUpdate) + 2, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ErrorScheme };
            btnDelete.Clicked += OnDelete;

            var btnBack = new Button("_Back") { X = 2, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnBack.Clicked += () => Application.RequestStop();

            Add(leftFrame, rightFrame, btnBack, btnGenerate, btnUpdate, btnDelete);

            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    allEmployees = db.Employees.OrderBy(e => e.Name).ToList();
                    var names = allEmployees.Select(e => $"{e.Name} (Borrow: {e.Borrow:N0})").ToList();
                    if (names.Count == 0) names.Add("No employees found");
                    employeeList.SetSource(names);
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnEmployeeSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < allEmployees.Count)
            {
                selectedEmployee = allEmployees[args.Item];
                LoadVouchers();
            }
            else
            {
                selectedEmployee = null;
                voucherList.SetSource(new List<string>());
            }
        }

        private void LoadVouchers()
        {
            if (selectedEmployee == null) return;
            try
            {
                using (var db = new AppDbContext())
                {
                    employeeVouchers = db.Vouchers
                        .Where(v => v.EmployeeId == selectedEmployee.Id)
                        .OrderByDescending(v => v.VoucherDate)
                        .ToList();

                    var display = employeeVouchers.Select(v =>
                        string.Format("{0:yyyy-MM-dd}   | {1,10:N2} | {2}", v.VoucherDate, v.Amount, v.Reason)
                    ).ToList();

                    if (display.Count == 0) display.Add("No vouchers found.");
                    voucherList.SetSource(display);
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnGenerate()
        {
            if (selectedEmployee == null) { Program.ShowError("Error", "Select an employee."); return; }
            Program.OpenModal(new VoucherDetailsWindow(selectedEmployee));
            // Refresh everything (borrow changed, vouchers added)
            LoadEmployees();
            // Reselect logic roughly
            employeeList.SelectedItem = allEmployees.FindIndex(e => e.Id == selectedEmployee.Id);
            LoadVouchers();
        }

        private void OnUpdate()
        {
            if (voucherList.SelectedItem < 0 || voucherList.SelectedItem >= employeeVouchers.Count) return;
            var voucher = employeeVouchers[voucherList.SelectedItem];
            Program.OpenModal(new VoucherDetailsWindow(voucher));
            LoadEmployees();
            LoadVouchers();
        }

        private void OnDelete()
        {
            if (voucherList.SelectedItem < 0 || voucherList.SelectedItem >= employeeVouchers.Count) return;
            var voucher = employeeVouchers[voucherList.SelectedItem];

            if (Program.ShowQuery("Confirm Delete", $"Delete voucher for {voucher.Amount:C}?"))
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Update Employee Borrow (Reduce debt)
                        var emp = db.Employees.Find(voucher.EmployeeId);
                        if (emp != null) emp.Borrow -= voucher.Amount;

                        db.Vouchers.Remove(voucher);
                        db.SaveChanges();
                    }
                    LoadEmployees();
                    LoadVouchers();
                }
                catch (Exception e) { Program.ShowError("Error", e.Message); }
            }
        }
    }
}