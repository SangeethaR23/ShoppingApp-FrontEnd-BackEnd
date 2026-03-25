namespace ShoppingWebApi.Models.DTOs.Wallet
{
    public class WalletTransactionReadDto
    {
        public int Id { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Type { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Remarks { get; set; }

    }
}
