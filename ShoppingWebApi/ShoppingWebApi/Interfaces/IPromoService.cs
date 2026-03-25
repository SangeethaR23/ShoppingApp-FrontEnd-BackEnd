using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Promo;

namespace ShoppingWebApi.Interfaces
{
    public interface IPromoService
    {
        Task<PromoCode?> GetValidPromoAsync(string code, decimal cartTotal, CancellationToken ct = default);
        Task<PromoCode> CreateAsync(PromoCreateDto dto, CancellationToken ct = default);
        Task<IEnumerable<PromoCode>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ActivateAsync(int id, bool isActive, CancellationToken ct = default);

    }
}
