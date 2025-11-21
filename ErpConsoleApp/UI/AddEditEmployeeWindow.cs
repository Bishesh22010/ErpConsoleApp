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
        private Employee employeeToEdit;

        public AddEditEmployeeWindow(Employee employee = null) : base(employee == null ? "Add New Employee" : "Edit Employee")
        {
            employeeToEdit = employee;
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center(); Y = Pos.Center(); Width = 50; Height = 18;
            Modal = true;

            var lblName = new Label("Name:") { X = 2, Y = 2 };
            nameField = new TextField(employee?.Name ?? "") { X = 15, Y = 2, Width = 30, ColorScheme = Colors.TextScheme };

            var lblMob = new Label("Mobile:") { X = 2, Y = 4 };
            mobileField = new TextField(employee?.MobNo ?? "") { X = 15, Y = 4, Width = 30, ColorScheme = Colors.TextScheme };

            var lblAddr = new Label("Address:") { X = 2, Y = 6 };
            addressField = new TextField(employee?.Address ?? "") { X = 15, Y = 6, Width = 30, ColorScheme = Colors.TextScheme };

            var lblSal = new Label("Salary:") { X = 2, Y = 8 };
            salaryField = new TextField(employee?.Salary.ToString() ?? "") { X = 15, Y = 8, Width = 30, ColorScheme = Colors.TextScheme };

            var btnSave = new Button("_Save") { X = Pos.Center() - 10, Y = 12, IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnSave.Clicked += OnSave;

            var btnCancel = new Button("_Cancel") { X = Pos.Center() + 5, Y = 12, ColorScheme = Colors.ButtonScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            Add(lblName, nameField, lblMob, mobileField, lblAddr, addressField, lblSal, salaryField, btnSave, btnCancel);
        }

        private void OnSave()
        {
            string name = nameField.Text.ToString();
            string mob = mobileField.Text.ToString();
            string addr = addressField.Text.ToString();

            if (string.IsNullOrWhiteSpace(name) || !decimal.TryParse(salaryField.Text.ToString(), out decimal salary))
            {
                Program.ShowError("Error", "Name and valid Salary are required.");
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
                            Borrow = 0
                        };
                        db.Employees.Add(newEmp);
                    }
                    else
                    {
                        // Update Existing
                        var emp = db.Employees.Find(employeeToEdit.Id);
                        if (emp != null)
                        {
                            emp.Name = name; emp.MobNo = mob; emp.Address = addr; emp.Salary = salary;
                        }
                    }
                    db.SaveChanges();
                }
                Application.RequestStop();
            }
            catch (Exception e)
            {
                // --- IMPROVED ERROR REPORTING ---
                // Show the InnerException which contains the actual DB error
                string msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                Program.ShowError("DB Error", msg);
            }
        }
    }
}