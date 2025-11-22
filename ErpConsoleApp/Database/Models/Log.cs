using System;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Module { get; set; }
        public string Action { get; set; }
        public int BranchId { get; set; } // Kept for compatibility
    }
}