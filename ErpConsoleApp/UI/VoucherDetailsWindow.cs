using System;
using Terminal.Gui;
using ErpConsoleApp.Database;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.UI
{
    public class VoucherDetailsWindow : Window
    {
        private TextField amountField;
        private TextField reasonField;
        private DateField dateField;

        private Employee selectedEmployee;
        private Voucher voucherToEdit;

        // Constructor for Adding New
        public VoucherDetailsWindow(Employee employee) : base($"New Voucher for {employee.Name}")
        {
            selectedEmployee = employee;
            InitView();
        }

        // Constructor for Editing
        public VoucherDetailsWindow(Voucher voucher) : base($"Edit Voucher (ID: {voucher.Id})")
        {
            voucherToEdit = voucher;
            // We load the employee from the voucher object
            using (var db = new AppDbContext())
            {
                selectedEmployee = db.Employees.Find(voucher.EmployeeId);
            }
            InitView();
        }

        private void InitView()
        {
            ColorScheme = Colors.WindowScheme;
            X = Pos.Center(); Y = Pos.Center(); Width = 50; Height = 16;
            Modal = true;

            int y = 2;

            Add(new Label("Date:") { X = 2, Y = y });
            dateField = new DateField(voucherToEdit?.VoucherDate ?? DateTime.Now)
            { X = 15, Y = y, Width = 20, ColorScheme = Colors.TextScheme };
            Add(dateField);
            y += 2;

            Add(new Label("Amount:") { X = 2, Y = y });
            amountField = new TextField(voucherToEdit?.Amount.ToString() ?? "")
            { X = 15, Y = y, Width = 20, ColorScheme = Colors.TextScheme };
            Add(amountField);
            y += 2;

            Add(new Label("Reason:") { X = 2, Y = y });
            reasonField = new TextField(voucherToEdit?.Reason ?? "")
            { X = 15, Y = y, Width = 30, ColorScheme = Colors.TextScheme };
            Add(reasonField);
            y += 3;

            var btnSave = new Button("_Save") { X = Pos.Center() - 10, Y = y, IsDefault = true, ColorScheme = Colors.ButtonScheme };
            btnSave.Clicked += OnSave;

            var btnCancel = new Button("_Cancel") { X = Pos.Center() + 5, Y = y, ColorScheme = Colors.ButtonScheme };
            btnCancel.Clicked += () => Application.RequestStop();

            Add(btnSave, btnCancel);
        }

        private void OnSave()
        {
            if (!decimal.TryParse(amountField.Text.ToString(), out decimal amount) || amount <= 0)
            {
                Program.ShowError("Error", "Invalid Amount."); return;
            }
            string reason = reasonField.Text.ToString();

            try
            {
                using (var db = new AppDbContext())
                {
                    var emp = db.Employees.Find(selectedEmployee.Id);
                    if (emp == null) { Program.ShowError("Error", "Employee not found."); return; }

                    if (voucherToEdit == null)
                    {
                        // --- CREATE NEW VOUCHER ---
                        var voucher = new Voucher
                        {
                            EmployeeId = emp.Id,
                            VoucherDate = dateField.Date,
                            Amount = amount,
                            Reason = reason
                        };
                        db.Vouchers.Add(voucher);

                        // Increase Borrow Balance
                        emp.Borrow += amount;
                    }
                    else
                    {
                        // --- UPDATE EXISTING VOUCHER ---
                        var voucher = db.Vouchers.Find(voucherToEdit.Id);
                        if (voucher != null)
                        {
                            // Reverse old amount, add new amount
                            emp.Borrow -= voucher.Amount;

                            voucher.VoucherDate = dateField.Date;
                            voucher.Amount = amount;
                            voucher.Reason = reason;

                            emp.Borrow += amount;
                        }
                    }
                    db.SaveChanges();
                }
                Program.ShowMessage("Success", "Voucher Saved.");
                Application.RequestStop();
            }
            catch (Exception e) { Program.ShowError("DB Error", e.Message); }
        }
    }
}