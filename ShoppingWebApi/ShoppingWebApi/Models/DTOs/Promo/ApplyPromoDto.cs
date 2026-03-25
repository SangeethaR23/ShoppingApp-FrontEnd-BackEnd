namespace ShoppingWebApi.Models.DTOs.Promo
{
    public class ApplyPromoDto
    {

        public string PromoCode { get; set; } = null!;
        public decimal CartTotal { get; set; }

    }
}
