using System.ComponentModel.DataAnnotations;

namespace ShoppingWebApi.Models
{
        public class PromoCode : BaseEntity
        {
            [Required, MaxLength(50)]
            public string Code { get; set; } = null!;  // e.g. FLAT200

            [Range(1, double.MaxValue)]
            public decimal DiscountAmount { get; set; }

            public bool IsActive { get; set; } = true;

            public DateTime StartDateUtc { get; set; }
            public DateTime EndDateUtc { get; set; }

            public decimal? MinOrderAmount { get; set; }
        }
    
}
