using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Cart
{
    public class CartAddItemDto
    {
        
            [Required] public int ProductId { get; set; }
            [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
        
    }
}
