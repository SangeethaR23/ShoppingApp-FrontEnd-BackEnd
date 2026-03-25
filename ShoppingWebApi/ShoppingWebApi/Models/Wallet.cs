using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models
{
    public class Wallet:BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Balance { get; set; } = 0m;

    }
}
