using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class Item
    {
        [Key]
        public int ItemId { get; set; } // Auto Increment

        [Required]
        public string ItemCode { get; set; } // New: Manually entered code

        [Required]
        public string ItemName { get; set; }
    }
}