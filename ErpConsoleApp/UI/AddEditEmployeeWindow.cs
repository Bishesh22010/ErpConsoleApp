using System;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class AddEditEmployeeWindow : Window
    {
        private TextField nameField;
        private TextField mobileField;
        private TextField addressField;
        private TextField salaryField;
        // --- NEW FIELD ---
        private TextField borrowField;
        private Employee employeeToEdit;

        public AddEditEmployeeWindow(Employee employee = null) : base(employee == null ? "Add New Employee" : "Edit Employee")
        {
            employeeToEdit = employee;
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center(); Y = Pos.Center(); Width = 50; Height = 20; // Increased height
            Modal = true;

            int y = 2;

            var lblName = new Label("Name:") { X = 2, Y = y };
            nameField = new TextField(employee?.Name ?? "") { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            y += 2;

            var lblMob = new Label("Mobile:") { X = 2, Y = y };
            mobileField = new TextField(employee?.MobNo ?? "") { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            y += 2;

            var lblAddr = new Label("Address:") { X = 2, Y = y };
            addressField = new TextField(employee?.Address ?? "") { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            y += 2;

            var lblSal = new Label("Salary:") { X = 2, Y = y };
            salaryField = new TextField(employee?.Salary.ToString() ?? "") { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            y += 2;

            // --- NEW UI ELEMENT FOR BORROW ---
            var lblBorrow = new Label("Init Borrow:") { X = 2, Y = y };
            borrowField = new TextField(employee?.Borrow.ToString() ?? "0") { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            y += 3; // Add extra space before buttons

            var btnSave = new Button("_Save") { X = Pos.Center() - 10, Y = y, IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnSave.Clicked += OnSave;

            var btnCancel = new Button("_Cancel") { X = Pos.Center() + 5, Y = y, ColorScheme = Colors.ButtonScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            Add(lblName, nameField, lblMob, mobileField, lblAddr, addressField, lblSal, salaryField, lblBorrow, borrowField, btnSave, btnCancel);
        }

        private void OnSave()
        {
            string name = nameField.Text.ToString();
            string mob = mobileField.Text.ToString();
            string addr = addressField.Text.ToString();

            // Validation
            if (string.IsNullOrWhiteSpace(name))
            {
                Program.ShowError("Error", "Name is required.");
                return;
            }
            if (!decimal.TryParse(salaryField.Text.ToString(), out decimal salary))
            {
                Program.ShowError("Error", "Invalid Salary.");
                return;
            }
            // --- NEW VALIDATION ---
            if (!decimal.TryParse(borrowField.Text.ToString(), out decimal borrow))
            {
                Program.ShowError("Error", "Invalid Borrow Amount.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    if (employeeToEdit == null)
                    {
                        // Add New
                        var newEmp = new Employee
                        {
                            Name = name,
                            MobNo = mob,
                            Address = addr,
                            Salary = salary,
                            Borrow = borrow // --- SAVE NEW VALUE ---
                        };
                        db.Employees.Add(newEmp);
                    }
                    else
                    {
                        // Update Existing
                        var emp = db.Employees.Find(employeeToEdit.Id);
                        if (emp != null)
                        {
                            emp.Name = name;
                            emp.MobNo = mob;
                            emp.Address = addr;
                            emp.Salary = salary;
                            emp.Borrow = borrow; // --- UPDATE EXISTING VALUE ---
                        }
                    }
                    db.SaveChanges();
                }
                Application.RequestStop();
            }
            catch (Exception e)
            {
                string msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                Program.ShowError("DB Error", msg);
            }
        }
    }
}