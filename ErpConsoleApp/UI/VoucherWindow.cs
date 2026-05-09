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
        private List<Employee> employees = new List<Employee>();
        private Employee selectedEmployee = null;

        private TextField dateField;
        private TextField amountField;
        private TextField reasonField;
        private TextField currentBorrowField;

        public VoucherWindow() : base("Issue Voucher (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
            Modal = true;

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            var leftPane = new FrameView("Select Employee") { X = 0, Y = 0, Width = Dim.Percent(35), Height = Dim.Fill(2) };
            employeeList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            employeeList.SelectedItemChanged += OnEmployeeSelected;
            leftPane.Add(employeeList);

            var rightPane = new FrameView("Voucher Details") { X = Pos.Right(leftPane), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2) };

            int y = 2;
            rightPane.Add(new Label("Current Borrow:") { X = 2, Y = y });
            currentBorrowField = new TextField("") { X = 18, Y = y, Width = 25, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightPane.Add(currentBorrowField); y += 3;

            rightPane.Add(new Label("Date (DD-MM-YYYY):") { X = 2, Y = y });
            dateField = new TextField(DateTime.Now.ToString("dd-MM-yyyy")) { X = 22, Y = y, Width = 21, ColorScheme = Colors.TextScheme };
            rightPane.Add(dateField); y += 2;

            rightPane.Add(new Label("Amount:") { X = 2, Y = y });
            amountField = new TextField("") { X = 18, Y = y, Width = 25, ColorScheme = Colors.TextScheme };
            rightPane.Add(amountField); y += 2;

            rightPane.Add(new Label("Reason:") { X = 2, Y = y });
            reasonField = new TextField("") { X = 18, Y = y, Width = Dim.Fill(2), ColorScheme = Colors.TextScheme };
            rightPane.Add(reasonField); y += 4;

            var btnSave = new Button("_Save Voucher") { X = 5, Y = y, ColorScheme = Colors.ButtonScheme, IsDefault = true };
            btnSave.Clicked += OnSaveVoucher;

            // --- NEW UNDO BUTTON ---
            var btnUndo = new Button("U_ndo (Ctrl+Z)") { X = Pos.Right(btnSave) + 3, Y = y, ColorScheme = Colors.ButtonScheme };
            btnUndo.Clicked += UndoManager.Undo;

            rightPane.Add(btnSave, btnUndo);

            var btnClose = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnClose.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+S] Save Voucher | [Ctrl+Z] Undo | [Alt+B]/[ESC] Back | [Tab] Navigate")
            { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ResultScheme };

            Add(leftPane, rightPane, btnClose, shortcutsLabel);
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            // Remember the currently selected employee ID so we can restore it after reload
            int? prevSelectedId = selectedEmployee?.Id;

            try
            {
                using (var db = new AppDbContext())
                {
                    employees = db.Employees.OrderBy(e => e.Name).ToList();
                    employeeList.SetSource(employees.Select(e => e.Name).ToList());

                    // If an employee was previously selected, restore the selection and update their latest borrow balance
                    if (prevSelectedId.HasValue)
                    {
                        int index = employees.FindIndex(e => e.Id == prevSelectedId.Value);
                        if (index >= 0)
                        {
                            employeeList.SelectedItem = index;
                            selectedEmployee = employees[index];
                            currentBorrowField.Text = selectedEmployee.Borrow.ToString("F2");
                        }
                    }
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnEmployeeSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < employees.Count)
            {
                selectedEmployee = employees[args.Item];
                currentBorrowField.Text = selectedEmployee.Borrow.ToString("F2");
            }
        }

        private void OnSaveVoucher()
        {
            if (selectedEmployee == null) { MessageBox.Query("Error", "Please select an employee.", "Ok"); return; }
            if (!DateTime.TryParseExact(dateField.Text.ToString(), "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime vDate)) { MessageBox.Query("Error", "Invalid Date.", "Ok"); return; }
            if (!decimal.TryParse(amountField.Text.ToString(), out decimal amt) || amt <= 0) { MessageBox.Query("Error", "Invalid Amount.", "Ok"); return; }

            string reason = reasonField.Text.ToString().Trim();
            if (string.IsNullOrEmpty(reason)) { MessageBox.Query("Error", "Reason is required.", "Ok"); return; }

            if (MessageBox.Query("Confirm", $"Add ₹{amt:N2} to {selectedEmployee.Name}'s borrow balance?", "Yes", "No") != 0) return;

            try
            {
                int newVoucherId;
                int empId = selectedEmployee.Id;
                decimal amountAdded = amt;

                using (var db = new AppDbContext())
                {
                    var voucher = new Voucher { EmployeeId = empId, VoucherDate = vDate, Amount = amt, Reason = reason };
                    db.Vouchers.Add(voucher);
                    var emp = db.Employees.Find(empId);
                    if (emp != null) emp.Borrow += amt;
                    db.SaveChanges();

                    newVoucherId = voucher.Id;
                }

                // --- PUSH TO UNDO MANAGER ---
                UndoManager.Push($"Voucher of ₹{amt:N2} for {selectedEmployee.Name}", () => {
                    using (var dbUndo = new AppDbContext())
                    {
                        // Delete the voucher record
                        var v = dbUndo.Vouchers.Find(newVoucherId);
                        if (v != null) dbUndo.Vouchers.Remove(v);

                        // Deduct the money back out of the employee's balance
                        var e = dbUndo.Employees.Find(empId);
                        if (e != null) e.Borrow -= amountAdded;

                        dbUndo.SaveChanges();
                    }
                }, () => {
                    LoadEmployees(); // Force a full UI refresh on undo to update the balance shown
                });

                MessageBox.Query("Success", "Voucher saved successfully.", "Ok");
                amountField.Text = ""; reasonField.Text = "";
                LoadEmployees();

            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}