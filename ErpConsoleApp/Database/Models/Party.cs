using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    /// <summary>
    /// Defines the structure of the Party (supplier/vendor)
    /// </summary>
    public class Party
    {
        [Key]
        public int PartyId { get; set; }
        [Required]
        public string Name { get; set; }

        // Navigation property: One Party can have many PurchaseSlips
        public virtual ICollection<PurchaseSlip> PurchaseSlips { get; set; }
    }
}