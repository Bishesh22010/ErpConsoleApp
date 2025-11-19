using System;
using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    /// <summary>
    /// Defines the structure of a single Purchase Slip
    /// </summary>
    public class PurchaseSlip
    {
        [Key]
        public int PurchaseSlipId { get; set; }
        public DateTime SlipDate { get; set; }
        [Required]
        public string ItemName { get; set; }
        public decimal Amount { get; set; }

        // Tracks if the slip is fully cleared
        public bool IsPaid { get; set; } = false;

        // --- NEW FIELD: Tracks partial payments ---
        public decimal PaidAmount { get; set; } = 0;

        // Foreign Key relationship
        public int PartyId { get; set; }
        public virtual Party Party { get; set; }
    }
}