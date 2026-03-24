using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace ShoppingWebApi.Models
{
    public class User : BaseEntity
    {
        [Required, MaxLength(256)]
        public string Email { get; set; } = null!; // unique

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(100)]
        public string? Role { get; set; } // "Admin"/"User" (optional)

        public UserDetails? UserDetails { get; set; }
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public Carts? Cart { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();


        public ICollection<Payment>? Payments { get; set; } = new List<Payment>();
        public ICollection<Refund>? Refunds { get; set; } = new List<Refund>();
        public ICollection<Order>? Orders { get; set; } = new List<Order>();
    }
}