namespace ShoppingWebApi.Models
{
    public class Refund
    {
        public int RefundId { get; set; }

        // FK to User — kept for direct lookup, but cascade is NoAction (break the cycle)
        public int UserId { get; set; }

        public int OrderId { get; set; }

        public int PaymentId { get; set; }

        public decimal RefundAmount { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Order? Order { get; set; }
        public Payment? Payment { get; set; }
    }
}