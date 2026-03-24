using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models.DTOs.Categories
{
      public class CategoryUpdateDto : CategoryCreateDto
        {
            [Required]
            public int Id { get; set; }
        }
    
}
