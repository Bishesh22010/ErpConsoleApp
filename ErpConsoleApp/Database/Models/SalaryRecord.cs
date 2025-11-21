using System;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class SalaryRecord
    {
        [Key]
        public int SalaryId { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public DateTime PaymentDate { get; set; } // The month being paid for
        public DateTime CalculationDate { get; set; } // When the entry was made

        public decimal SalaryAmount { get; set; } // Base Monthly Salary
        public decimal PresentDays { get; set; }
        public decimal AbsentDays { get; set; }

        public decimal DeductionPerDay { get; set; }
        public decimal TotalDeduction { get; set; } // Absence + Borrow repaid
        public decimal BorrowRepayment { get; set; } // How much borrow was paid back

        public decimal FinalSalary { get; set; } // The actual amount paid
    }
}