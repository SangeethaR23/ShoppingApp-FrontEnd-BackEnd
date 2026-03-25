namespace ShoppingWebApi.Models.DTOs.Wishlist
{
    public class WishlistReadDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string SKU { get; set; } = null!;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

    }
}
