using System;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    /// <summary>
    /// Defines the structure of a single Purchase Slip Item
    /// </summary>
    public class PurchaseSlip
    {
        [Key]
        public int PurchaseSlipId { get; set; }

        // --- NEW FIELDS TO MATCH HANDWRITTEN SLIP ---
        public int SlipNumber { get; set; } // Auto-incrementing ID grouping items together
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public string QtyType { get; set; }
        public decimal UnitPrice { get; set; }
        // --------------------------------------------

        public DateTime SlipDate { get; set; }
        [Required]
        public string ItemName { get; set; }
        public decimal Amount { get; set; }

        // Tracks if the slip is fully cleared
        public bool IsPaid { get; set; } = false;

        // Tracks partial payments
        public decimal PaidAmount { get; set; } = 0;

        // Foreign Key relationship
        public int PartyId { get; set; }
        public virtual Party Party { get; set; }
    }
}