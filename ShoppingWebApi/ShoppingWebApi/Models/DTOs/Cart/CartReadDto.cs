using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Cart
{
    public class CartReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<CartItemReadDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
    }


}
