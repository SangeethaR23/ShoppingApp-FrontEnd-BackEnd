namespace ShoppingWebApi.Models.DTOs.Categories
{
    public class CategoryReadDto
    {
        
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
            public int? ParentCategoryId { get; set; }
            public DateTime CreatedUtc { get; set; }
            public DateTime? UpdatedUtc { get; set; }
        }
    
}
