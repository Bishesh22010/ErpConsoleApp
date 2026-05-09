using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class SalaryWindow : Window
    {
        private ListView employeeList;
        private List<Employee> allEmployees = new List<Employee>();
        private Employee selectedEmployee = null;

        private ComboBox monthCombo;
        private TextField yearField;
        private TextField monthlySalaryField;
        private TextField currentBorrowField;
        private TextField presentDaysField;
        private TextField absentDaysField;
        private TextField perDayRateField;
        private TextField payableSalaryField;
        private TextField borrowLeftField;

        public SalaryWindow() : base("Salary Management (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            // --- GLOBAL CTRL+Z AND ESCAPE HANDLER ---
            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
                else if (e.KeyEvent.Key == (Key.Z | Key.CtrlMask)) { UndoManager.Undo(); e.Handled = true; }
            };

            var leftFrame = new FrameView("Select Employee") { X = 0, Y = 0, Width = Dim.Percent(30), Height = Dim.Fill(2) };
            employeeList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            employeeList.SelectedItemChanged += OnEmployeeSelected;
            leftFrame.Add(employeeList);

            var rightFrame = new FrameView("Salary Details") { X = Pos.Right(leftFrame), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2) };

            int y = 1; int xLabels = 2; int xFields = 20; int fieldWidth = 20;

            rightFrame.Add(new Label("Process Date:") { X = xLabels, Y = y });
            var processDateField = new TextField(DateTime.Now.ToString("dd-MM-yyyy")) { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(processDateField);
            y += 2;

            rightFrame.Add(new Label("Salary For:") { X = xLabels, Y = y });
            monthCombo = new ComboBox() { X = xFields, Y = y, Width = 10, Height = 6, ColorScheme = Colors.TextScheme };
            monthCombo.SetSource(new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" });
            monthCombo.SelectedItem = DateTime.Now.Month - 1;
            monthCombo.SelectedItemChanged += (e) => { UpdateDefaultDays(); Recalculate(); };

            yearField = new TextField(DateTime.Now.Year.ToString()) { X = xFields + 12, Y = y, Width = 8, ColorScheme = Colors.TextScheme };
            yearField.TextChanged += (t) => { UpdateDefaultDays(); Recalculate(); };
            rightFrame.Add(monthCombo, yearField);
            y += 2;

            rightFrame.Add(new Label("Monthly Salary:") { X = xLabels, Y = y });
            monthlySalaryField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(monthlySalaryField);
            y += 2;

            rightFrame.Add(new Label("Current Borrow:") { X = xLabels, Y = y });
            currentBorrowField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(currentBorrowField);
            y += 2;

            rightFrame.Add(new Label("Present Days:") { X = xLabels, Y = y });
            presentDaysField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ColorScheme = Colors.TextScheme };
            presentDaysField.TextChanged += (t) => Recalculate();
            rightFrame.Add(presentDaysField);
            y += 2;

            rightFrame.Add(new Label("Absent Days:") { X = xLabels, Y = y });
            absentDaysField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(absentDaysField);
            y += 2;

            rightFrame.Add(new Label("Per-Day Rate:") { X = xLabels, Y = y });
            perDayRateField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(perDayRateField);
            y += 3;

            rightFrame.Add(new Label("PAYABLE SALARY:") { X = xLabels, Y = y, ColorScheme = Colors.ResultScheme });
            payableSalaryField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(payableSalaryField);
            y += 2;

            rightFrame.Add(new Label("Borrow Left:") { X = xLabels, Y = y });
            borrowLeftField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(borrowLeftField);
            y += 4;

            var btnCalculate = new Button("_Calculate & Save") { X = 5, Y = y, ColorScheme = Colors.ButtonScheme };
            btnCalculate.Clicked += OnCalculateAndSave;

            var btnHistory = new Button("View _History") { X = Pos.Right(btnCalculate) + 2, Y = y, ColorScheme = Colors.ButtonScheme };
            btnHistory.Clicked += OnViewHistory;

            // --- NEW UNDO BUTTON ---
            var btnUndo = new Button("U_ndo (Ctrl+Z)") { X = Pos.Right(btnHistory) + 2, Y = y, ColorScheme = Colors.ButtonScheme };
            btnUndo.Clicked += UndoManager.Undo;

            rightFrame.Add(btnCalculate, btnHistory, btnUndo);

            var btnClose = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(2), ColorScheme = Colors.ButtonScheme };
            btnClose.Clicked += () => Application.RequestStop();

            var shortcutsLabel = new Label("Shortcuts: [Alt+C] Calculate | [Alt+H] History | [Ctrl+Z] Undo | [Alt+B] Back | [Tab] Navigate")
            { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ResultScheme };

            Add(leftFrame, rightFrame, btnClose, shortcutsLabel);
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    allEmployees = db.Employees.OrderBy(e => e.Name).ToList();
                    var names = allEmployees.Select(e => e.Name).ToList();
                    if (names.Count == 0) names.Add("No employees found");
                    employeeList.SetSource(names);
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void UpdateDefaultDays()
        {
            if (selectedEmployee == null || monthCombo.SelectedItem < 0) return;
            if (!int.TryParse(yearField.Text?.ToString(), out int year) || year < 2000 || year > 2100) return;

            int month = monthCombo.SelectedItem + 1;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            presentDaysField.Text = daysInMonth.ToString();
        }

        private void OnEmployeeSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < allEmployees.Count)
            {
                selectedEmployee = allEmployees[args.Item];
                monthlySalaryField.Text = selectedEmployee.Salary.ToString("F2");
                currentBorrowField.Text = selectedEmployee.Borrow.ToString("F2");
                UpdateDefaultDays(); Recalculate();
            }
        }

        private void Recalculate()
        {
            if (selectedEmployee == null || monthCombo.SelectedItem < 0) return;
            try
            {
                if (!int.TryParse(yearField.Text?.ToString(), out int year) || year < 2000 || year > 2100) return;
                int month = monthCombo.SelectedItem + 1;
                int daysInMonth = DateTime.DaysInMonth(year, month);

                if (!decimal.TryParse(presentDaysField.Text.ToString(), out decimal presentDays)) return;
                if (presentDays < 0 || presentDays > daysInMonth) { payableSalaryField.Text = "Invalid Days"; return; }

                decimal perDayRate = selectedEmployee.Salary / 30m;
                decimal totalEarnedSalary = (presentDays == daysInMonth) ? selectedEmployee.Salary + ((daysInMonth - 30) * perDayRate) : presentDays * perDayRate;

                decimal borrowRepayment = Math.Min(selectedEmployee.Borrow, Math.Max(0, totalEarnedSalary));
                decimal finalSalary = totalEarnedSalary - borrowRepayment;
                decimal newBorrow = selectedEmployee.Borrow - borrowRepayment;

                absentDaysField.Text = (daysInMonth - presentDays).ToString("0.##");
                perDayRateField.Text = perDayRate.ToString("F2");
                payableSalaryField.Text = finalSalary.ToString("F2");
                borrowLeftField.Text = newBorrow.ToString("F2");
            }
            catch { }
        }

        private void OnCalculateAndSave()
        {
            if (selectedEmployee == null) { Program.ShowError("Error", "Select employee."); return; }
            if (monthCombo.SelectedItem < 0) { Program.ShowError("Error", "Select a valid month."); return; }
            if (!int.TryParse(yearField.Text?.ToString(), out int year) || year < 2000 || year > 2100) { Program.ShowError("Error", "Enter a valid year."); return; }

            int month = monthCombo.SelectedItem + 1;
            int daysInMonth = DateTime.DaysInMonth(year, month);

            if (!decimal.TryParse(presentDaysField.Text.ToString(), out decimal presentDays) || presentDays < 0 || presentDays > daysInMonth)
            { Program.ShowError("Error", "Invalid present days."); return; }

            decimal monthlySalary = selectedEmployee.Salary;
            decimal currentBorrow = selectedEmployee.Borrow;
            decimal perDayRate = monthlySalary / 30m;
            decimal totalEarnedSalary = (presentDays == daysInMonth) ? monthlySalary + ((daysInMonth - 30) * perDayRate) : presentDays * perDayRate;
            decimal borrowRepayment = Math.Min(currentBorrow, Math.Max(0, totalEarnedSalary));
            decimal finalSalary = totalEarnedSalary - borrowRepayment;
            decimal newBorrow = currentBorrow - borrowRepayment;

            if (!Program.ShowQuery("Confirm", $"Process Salary: {finalSalary:F2}?\nBorrow Repaid: {borrowRepayment:F2}")) return;

            try
            {
                DateTime targetMonth = new DateTime(year, month, 1);
                DateTime processingDate = DateTime.Now;

                using (var db = new AppDbContext())
                {
                    var existing = db.Salaries.FirstOrDefault(s => s.EmployeeId == selectedEmployee.Id && s.PaymentDate.Month == month && s.PaymentDate.Year == year);
                    if (existing != null) { Program.ShowError("Error", "Salary already paid for this month."); return; }

                    var record = new SalaryRecord
                    {
                        EmployeeId = selectedEmployee.Id,
                        PaymentDate = targetMonth,
                        CalculationDate = processingDate,
                        SalaryAmount = monthlySalary,
                        PresentDays = presentDays,
                        AbsentDays = daysInMonth - presentDays,
                        DeductionPerDay = perDayRate,
                        BorrowRepayment = borrowRepayment,
                        TotalDeduction = borrowRepayment,
                        FinalSalary = finalSalary
                    };
                    db.Salaries.Add(record);

                    var emp = db.Employees.Find(selectedEmployee.Id);
                    if (emp != null) emp.Borrow = newBorrow;

                    db.SaveChanges();
                }

                // --- PUSH TO UNDO MANAGER ---
                int empIdForUndo = selectedEmployee.Id;
                decimal oldBorrowAmount = currentBorrow; // The borrow amount BEFORE we reduced it

                UndoManager.Push($"Processed Salary for {selectedEmployee.Name} ({targetMonth:MMM yyyy})", () => {
                    using (var dbUndo = new AppDbContext())
                    {
                        // Find and delete the exact salary record we just created
                        var rec = dbUndo.Salaries.FirstOrDefault(s => s.EmployeeId == empIdForUndo && s.CalculationDate == processingDate);
                        if (rec != null) dbUndo.Salaries.Remove(rec);

                        // Restore the employee's old borrow balance
                        var emp = dbUndo.Employees.Find(empIdForUndo);
                        if (emp != null) emp.Borrow = oldBorrowAmount;

                        dbUndo.SaveChanges();
                    }
                }, () => {
                    // Update UI automatically if undone
                    selectedEmployee.Borrow = oldBorrowAmount;
                    currentBorrowField.Text = oldBorrowAmount.ToString("F2");
                    Recalculate();
                });

                Program.ShowMessage("Success", "Salary Processed.");
                selectedEmployee.Borrow = newBorrow;
                currentBorrowField.Text = newBorrow.ToString("F2");
                Recalculate();

            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnViewHistory()
        {
            if (selectedEmployee == null) return;
            Program.OpenModal(new SalaryHistoryWindow(selectedEmployee));
        }
    }
}