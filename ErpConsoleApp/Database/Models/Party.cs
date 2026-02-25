using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class Party
    {
        [Key]
        public int PartyId { get; set; }

        [Required]
        public string Name { get; set; }

        // New Fields
        public string GstNumber { get; set; } // Nullable/Optional by default
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        // Navigation property for related slips
        public virtual ICollection<PurchaseSlip> PurchaseSlips { get; set; } = new List<PurchaseSlip>();
    }
}