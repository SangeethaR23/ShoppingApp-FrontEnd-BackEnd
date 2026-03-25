using ShoppingWebApi.Models.enums;
using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models
{
    public class ReturnRequest : BaseEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [MaxLength(1000)]
        public string Reason { get; set; } = null!;

        [MaxLength(2000)]
        public string? Comments { get; set; }

        public ReturnStatus Status { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }
    }
}
