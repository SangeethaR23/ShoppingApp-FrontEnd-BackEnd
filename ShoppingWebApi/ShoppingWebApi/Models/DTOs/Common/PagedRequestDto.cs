namespace ShoppingWebApi.Models.DTOs.Common
{
    public class PagedRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
        public string? SortBy { get; set; } = "name";
        public string? SortDir { get; set; } = "asc";
    }
}