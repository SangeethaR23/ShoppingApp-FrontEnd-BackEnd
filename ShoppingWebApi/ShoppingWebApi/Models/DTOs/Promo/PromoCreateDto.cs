namespace ShoppingWebApi.Models.DTOs.Promo
{
    public class PromoCreateDto
    {
        public string Code { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public decimal? MinOrderAmount { get; set; }

    }
}
