using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Orders
{
    public class PlaceOrderRequestDto
    {
        public int UserId { get; set; }
        public int AddressId { get; set; }
        public string? Notes { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? Discount { get; set; }

        [Required(ErrorMessage = "Payment type is required")]
        public string PaymentType { get; set; } = string.Empty;

        public decimal WalletUseAmount { get; set; } = 0m;
        public string? PromoCode { get; set; }
    }
}
