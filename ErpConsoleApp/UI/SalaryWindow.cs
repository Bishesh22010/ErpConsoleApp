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

        // Form Controls
        private DateField salaryMonthField;
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

            KeyDown += (e) => { if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; } };

            // --- Left Pane: Employees ---
            var leftFrame = new FrameView("Select Employee")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(30),
                Height = Dim.Fill()
            };
            employeeList = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            employeeList.SelectedItemChanged += OnEmployeeSelected;
            leftFrame.Add(employeeList);

            // --- Right Pane: Calculation ---
            var rightFrame = new FrameView("Salary Details")
            {
                X = Pos.Right(leftFrame),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            int y = 1;
            int xLabels = 2;
            int xFields = 20;
            int fieldWidth = 20;

            rightFrame.Add(new Label("Salary Month:") { X = xLabels, Y = y });
            salaryMonthField = new DateField(DateTime.Now) { X = xFields, Y = y, Width = fieldWidth, ColorScheme = Colors.TextScheme };
            // Trigger recalculation when date changes (to get days in month)
            salaryMonthField.DateChanged += (d) => Recalculate();
            rightFrame.Add(salaryMonthField);
            y += 2;

            // --- READ ONLY: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("Monthly Salary:") { X = xLabels, Y = y });
            monthlySalaryField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(monthlySalaryField);
            y += 2;

            // --- READ ONLY: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("Current Borrow:") { X = xLabels, Y = y });
            currentBorrowField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(currentBorrowField);
            y += 2;

            // --- EDITABLE: TextScheme (White) ---
            rightFrame.Add(new Label("Present Days:") { X = xLabels, Y = y });
            presentDaysField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ColorScheme = Colors.TextScheme };
            presentDaysField.TextChanged += (t) => Recalculate();
            rightFrame.Add(presentDaysField);
            y += 2;

            // --- READ ONLY: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("Absent Days:") { X = xLabels, Y = y });
            absentDaysField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(absentDaysField);
            y += 2;

            // --- READ ONLY: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("Per-Day Rate:") { X = xLabels, Y = y });
            perDayRateField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(perDayRateField);
            y += 3; // Extra space

            // --- RESULT: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("PAYABLE SALARY:") { X = xLabels, Y = y, ColorScheme = Colors.ResultScheme });
            payableSalaryField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(payableSalaryField);
            y += 2;

            // --- RESULT: ResultScheme (Yellow) ---
            rightFrame.Add(new Label("Borrow Left:") { X = xLabels, Y = y });
            borrowLeftField = new TextField("") { X = xFields, Y = y, Width = fieldWidth, ReadOnly = true, ColorScheme = Colors.ResultScheme };
            rightFrame.Add(borrowLeftField);
            y += 4;

            var btnCalculate = new Button("_Calculate & Save") { X = 5, Y = y, ColorScheme = Colors.ButtonScheme };
            btnCalculate.Clicked += OnCalculateAndSave;

            var btnHistory = new Button("View _History") { X = 30, Y = y, ColorScheme = Colors.ButtonScheme };
            btnHistory.Clicked += OnViewHistory;

            rightFrame.Add(btnCalculate, btnHistory);

            // Close Button
            var btnClose = new Button("_Back") { X = Pos.Center(), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnClose.Clicked += () => Application.RequestStop();

            Add(leftFrame, rightFrame, btnClose);
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

        private void OnEmployeeSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < allEmployees.Count)
            {
                selectedEmployee = allEmployees[args.Item];
                monthlySalaryField.Text = selectedEmployee.Salary.ToString("F2");
                currentBorrowField.Text = selectedEmployee.Borrow.ToString("F2");

                // Default present days to total days in month
                int daysInMonth = DateTime.DaysInMonth(salaryMonthField.Date.Year, salaryMonthField.Date.Month);
                presentDaysField.Text = daysInMonth.ToString();

                Recalculate();
            }
        }

        private void Recalculate()
        {
            if (selectedEmployee == null) return;

            try
            {
                int daysInMonth = DateTime.DaysInMonth(salaryMonthField.Date.Year, salaryMonthField.Date.Month);

                if (!decimal.TryParse(presentDaysField.Text.ToString(), out decimal presentDays)) return;
                if (presentDays < 0 || presentDays > daysInMonth)
                {
                    payableSalaryField.Text = "Invalid Days"; return;
                }

                decimal monthlySalary = selectedEmployee.Salary;
                decimal currentBorrow = selectedEmployee.Borrow;

                decimal perDayRate = monthlySalary / 30m;
                decimal salaryForDaysWorked = presentDays * perDayRate;

                decimal totalEarnedSalary = salaryForDaysWorked;
                if (presentDays == daysInMonth)
                {
                    int diff = daysInMonth - 30;
                    totalEarnedSalary = monthlySalary + (diff * perDayRate);
                }

                decimal borrowRepayment = Math.Min(currentBorrow, Math.Max(0, totalEarnedSalary));
                decimal finalSalary = totalEarnedSalary - borrowRepayment;
                decimal newBorrow = currentBorrow - borrowRepayment;

                // Update UI
                absentDaysField.Text = (daysInMonth - presentDays).ToString("0.##");
                perDayRateField.Text = perDayRate.ToString("F2");
                payableSalaryField.Text = finalSalary.ToString("F2");
                borrowLeftField.Text = newBorrow.ToString("F2");

            }
            catch { /* Ignore parse errors during typing */ }
        }

        private void OnCalculateAndSave()
        {
            if (selectedEmployee == null) { Program.ShowError("Error", "Select employee."); return; }

            int daysInMonth = DateTime.DaysInMonth(salaryMonthField.Date.Year, salaryMonthField.Date.Month);
            if (!decimal.TryParse(presentDaysField.Text.ToString(), out decimal presentDays) || presentDays > daysInMonth)
            {
                Program.ShowError("Error", "Invalid present days."); return;
            }

            decimal monthlySalary = selectedEmployee.Salary;
            decimal currentBorrow = selectedEmployee.Borrow;
            decimal perDayRate = monthlySalary / 30m;
            decimal totalEarnedSalary = (presentDays == daysInMonth)
                ? monthlySalary + ((daysInMonth - 30) * perDayRate)
                : presentDays * perDayRate;
            decimal borrowRepayment = Math.Min(currentBorrow, Math.Max(0, totalEarnedSalary));
            decimal finalSalary = totalEarnedSalary - borrowRepayment;
            decimal newBorrow = currentBorrow - borrowRepayment;

            if (!Program.ShowQuery("Confirm", $"Process Salary: {finalSalary:F2}?\nBorrow Repaid: {borrowRepayment:F2}")) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var existing = db.Salaries.FirstOrDefault(s =>
                        s.EmployeeId == selectedEmployee.Id &&
                        s.PaymentDate.Month == salaryMonthField.Date.Month &&
                        s.PaymentDate.Year == salaryMonthField.Date.Year);

                    if (existing != null) { Program.ShowError("Error", "Salary already paid for this month."); return; }

                    var record = new SalaryRecord
                    {
                        EmployeeId = selectedEmployee.Id,
                        PaymentDate = salaryMonthField.Date,
                        CalculationDate = DateTime.Now,
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
                Program.ShowMessage("Success", "Salary Processed.");

                selectedEmployee.Borrow = newBorrow;
                currentBorrowField.Text = newBorrow.ToString("F2");

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