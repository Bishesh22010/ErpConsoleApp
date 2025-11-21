using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        // Change these to 'string?' so the database allows NULL values
        public string? MobNo { get; set; }
        public string? Address { get; set; }
        
        public decimal Salary { get; set; }
        public decimal Borrow { get; set; } 
        
        public string? IdProofType { get; set; } 
        public string? IdProofFilePath { get; set; } 
    }
}