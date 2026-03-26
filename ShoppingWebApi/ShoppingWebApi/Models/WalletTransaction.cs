using ShoppingWebApi.Models.enums;

namespace ShoppingWebApi.Models
{
    public class WalletTransaction : BaseEntity
    {
        public int WalletId { get; set; }   // FK to Wallets.Id
        public Wallet Wallet { get; set; } = null!;

        // Keep UserId for direct user lookup (denormalized)
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal Amount { get; set; }
        public WalletTxnType Type { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Reference { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
