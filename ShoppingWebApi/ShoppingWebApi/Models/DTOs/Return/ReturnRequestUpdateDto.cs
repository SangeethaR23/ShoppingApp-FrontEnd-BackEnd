namespace ShoppingWebApi.Models.DTOs.Return
{
    public class ReturnRequestUpdateDto
    {

        public string Action { get; set; } = null!; // approve / reject
        public string? Comments { get; set; }

    }
}
