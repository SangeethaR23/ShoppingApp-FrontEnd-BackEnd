namespace ShoppingWebApi.Models.DTOs.Return
{
    public class ReturnRequestCreateDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = null!;

    }
}
