namespace ShoppingWebApi.Models.DTOs.Users
{
    public class ChangePasswordRequestDto
    {

       public int UserId { get; set; }         
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;

    }
}
