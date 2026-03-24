namespace ShoppingWebApi.Models.DTOs.Common
{
    public class OrderPagedRequestDto
    {
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? UserId { get; set; }

        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;

        public string? SortBy { get; set; } = "date";
        public bool Desc { get; set; } = true;

    }
}
