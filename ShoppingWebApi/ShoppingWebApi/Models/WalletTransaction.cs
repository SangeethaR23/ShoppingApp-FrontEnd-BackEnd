using ShoppingWebApi.Models.enums;
using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models
{
    public class WalletTransaction:BaseEntity
    {

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal Amount { get; set; } // +credit, -debit
        public WalletTxnType Type { get; set; }  // CreditRefund, DebitOrder, AdminAdjust

        [MaxLength(200)]
        public string? Reference { get; set; }   // e.g., "Order: 123", "Refund: 45"

        [MaxLength(500)]
        public string? Remarks { get; set; }




    }
}
