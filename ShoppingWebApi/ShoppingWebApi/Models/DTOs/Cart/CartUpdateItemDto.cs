using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Cart
{
    public class CartUpdateItemDto
    {
            [Required] public int ProductId { get; set; }
            [Range(0, int.MaxValue)] public int Quantity { get; set; } 
        
    }
}
