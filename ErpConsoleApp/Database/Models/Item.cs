using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErpConsoleApp.Database.Models
{
    public class Item
    {
        [Key]
        public int ItemId { get; set; } // Auto Increment

        [Required]
        public string ItemName { get; set; }
    }
}
