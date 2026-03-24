namespace ShoppingWebApi.Models.DTOs.Cart
{
    public class CartItemReadDto
    {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = null!;
            public string SKU { get; set; } = null!;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineTotal { get; set; }

            // For “check reviews in cart”
            public double AverageRating { get; set; }
            public int ReviewsCount { get; set; }
        

    }
}
