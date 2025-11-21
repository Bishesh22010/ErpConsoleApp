using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class ManageEmployeeWindow : Window
    {
        private ListView employeeList;
        private TextField searchField;
        private Label totalLabel;
        private List<Employee> allEmployees = new List<Employee>();

        public ManageEmployeeWindow() : base("Manage Employees (Press ESC to go back)")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            KeyDown += (e) => {
                if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
            };

            // --- Top: Search & Stats ---
            var topFrame = new FrameView("Search & Stats") { X = 0, Y = 0, Width = Dim.Fill(), Height = 5 };

            topFrame.Add(new Label("Search Name:") { X = 1, Y = 0 });
            searchField = new TextField("") { X = 14, Y = 0, Width = 30, ColorScheme = Colors.TextScheme };

            var btnSearch = new Button("_Search") { X = 46, Y = 0, ColorScheme = Colors.ButtonScheme };
            btnSearch.Clicked += () => LoadEmployees(searchField.Text.ToString());

            var btnReset = new Button("_Reset") { X = 58, Y = 0, ColorScheme = Colors.ButtonScheme };
            btnReset.Clicked += () => { searchField.Text = ""; LoadEmployees(); };

            totalLabel = new Label("Total: 0") { X = Pos.AnchorEnd(15), Y = 0, ColorScheme = Colors.WindowScheme };

            topFrame.Add(searchField, btnSearch, btnReset, totalLabel);

            // --- Middle: Employee List ---
            var listFrame = new FrameView("Employee List") { X = 0, Y = 5, Width = Dim.Fill(), Height = Dim.Fill(2) };

            // Header Label for columns
            var header = new Label(string.Format("{0,-5} | {1,-20} | {2,-15} | {3,10} | {4,10}", "ID", "Name", "Mobile", "Salary", "Borrow"))
            { X = 0, Y = 0, ColorScheme = Colors.MenuScheme }; // Use menu scheme for header look

            employeeList = new ListView() { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = Colors.TextScheme };
            listFrame.Add(header, employeeList);

            // --- Bottom: Actions ---
            var btnAdd = new Button("_Add Employee") { X = 2, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnAdd.Clicked += () => { Program.OpenModal(new AddEditEmployeeWindow()); LoadEmployees(); };

            var btnUpdate = new Button("_Update Selected") { X = 20, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnUpdate.Clicked += OnUpdate;

            var btnDelete = new Button("_Delete Selected") { X = 42, Y = Pos.AnchorEnd(1), ColorScheme = Colors.ErrorScheme };
            btnDelete.Clicked += OnDelete;

            var btnBack = new Button("_Back") { X = Pos.AnchorEnd(10), Y = Pos.AnchorEnd(1), ColorScheme = Colors.ButtonScheme };
            btnBack.Clicked += () => Application.RequestStop();

            Add(topFrame, listFrame, btnAdd, btnUpdate, btnDelete, btnBack);

            LoadEmployees();
        }

        private void LoadEmployees(string search = "")
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var query = db.Employees.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(e => e.Name.ToLower().Contains(search.ToLower()));
                    }

                    allEmployees = query.OrderBy(e => e.Name).ToList();

                    var displayList = allEmployees.Select(e =>
                        string.Format("{0,-5} | {1,-20} | {2,-15} | {3,10:N0} | {4,10:N0}",
                        e.Id, e.Name, e.MobNo, e.Salary, e.Borrow)).ToList();

                    if (displayList.Count == 0) displayList.Add("No employees found.");

                    employeeList.SetSource(displayList);
                    totalLabel.Text = $"Total: {allEmployees.Count}";
                }
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }

        private void OnUpdate()
        {
            if (employeeList.SelectedItem < 0 || employeeList.SelectedItem >= allEmployees.Count) return;
            var emp = allEmployees[employeeList.SelectedItem];
            Program.OpenModal(new AddEditEmployeeWindow(emp));
            LoadEmployees();
        }

        private void OnDelete()
        {
            if (employeeList.SelectedItem < 0 || employeeList.SelectedItem >= allEmployees.Count) return;
            var emp = allEmployees[employeeList.SelectedItem];

            if (Program.ShowQuery("Confirm", $"Delete {emp.Name}?"))
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var e = db.Employees.Find(emp.Id);
                        if (e != null) { db.Employees.Remove(e); db.SaveChanges(); }
                    }
                    LoadEmployees();
                }
                catch (Exception e) { Program.ShowError("Error", e.Message); }
            }
        }
    }
}