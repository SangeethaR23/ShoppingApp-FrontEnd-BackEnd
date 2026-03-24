using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Products
{
    public class ProductImageCreateDto
    {
        
            [Required, Url, MaxLength(2000)]
            public string Url { get; set; } = null!;
        
    }
}
