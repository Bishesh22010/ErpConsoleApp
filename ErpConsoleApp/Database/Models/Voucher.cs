using System;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string Reason { get; set; }
        public DateTime VoucherDate { get; set; }
    }
}