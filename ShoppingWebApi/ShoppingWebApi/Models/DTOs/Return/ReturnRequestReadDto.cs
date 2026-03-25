namespace ShoppingWebApi.Models.DTOs.Return
{
    public class ReturnRequestReadDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string? Comments { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }

    }
}
