using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Promo;

namespace ShoppingWebApi.Services
{
    public class PromoService : IPromoService
    {
        private readonly IRepository<int, PromoCode> _promoRepo;

        public PromoService(IRepository<int, PromoCode> promoRepo)
        {
            _promoRepo = promoRepo;
        }

        public async Task<PromoCode?> GetValidPromoAsync(string code, decimal cartTotal, CancellationToken ct = default)
        {
            var promo = await _promoRepo.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code && p.IsActive, ct);

            if (promo == null)
                return null;

            var now = DateTime.UtcNow;

            if (now < promo.StartDateUtc || now > promo.EndDateUtc)
                return null;

            if (promo.MinOrderAmount.HasValue && cartTotal < promo.MinOrderAmount.Value)
                return null;

            return promo;
        }

        public async Task<PromoCode> CreateAsync(PromoCreateDto dto, CancellationToken ct = default)
        {
            var promo = new PromoCode
            {
                Code = dto.Code.ToUpper(),
                DiscountAmount = dto.DiscountAmount,
                StartDateUtc = dto.StartDateUtc,
                EndDateUtc = dto.EndDateUtc,
                MinOrderAmount = dto.MinOrderAmount,
                IsActive = true
            };

            return await _promoRepo.Add(promo);
        }

        public async Task<IEnumerable<PromoCode>> GetAllAsync(CancellationToken ct = default)
        {
            return await _promoRepo.GetQueryable()
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync(ct);
        }

        public async Task<bool> ActivateAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var promo = await _promoRepo.Get(id);
            if (promo == null) return false;

            promo.IsActive = isActive;
            promo.UpdatedUtc = DateTime.UtcNow;

            await _promoRepo.Update(id, promo);
            return true;
        }
    }
}