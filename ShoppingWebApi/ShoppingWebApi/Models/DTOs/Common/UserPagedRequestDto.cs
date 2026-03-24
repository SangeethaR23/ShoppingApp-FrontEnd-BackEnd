namespace ShoppingWebApi.Models.DTOs.Common
{
    public class UserPagedRequestDto
    {

        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Name { get; set; }

        public string? SortBy { get; set; } = "date";
        public bool Desc { get; set; } = true;

        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;

    }
}
