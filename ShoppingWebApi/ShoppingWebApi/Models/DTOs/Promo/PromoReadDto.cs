namespace ShoppingWebApi.Models.DTOs.Promo
{
    public class PromoReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public bool IsActive { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

    }
}
