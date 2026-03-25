using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Promo;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/promo")]
    public class PromoController : ControllerBase
    {
        private readonly IPromoService _promoService;

        public PromoController(IPromoService promoService)
        {
            _promoService = promoService;
        }

        // ✅ ADMIN — CREATE PROMO
        [HttpPost("create")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(PromoCreateDto dto, CancellationToken ct)
        {
            var promo = await _promoService.CreateAsync(dto, ct);
            return Ok(promo);
        }

        // ✅ ADMIN — ACTIVATE/DEACTIVATE PROMO
        [HttpPost("{id}/activate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Activate(int id, bool active, CancellationToken ct)
        {
            var ok = await _promoService.ActivateAsync(id, active, ct);
            return ok ? Ok() : NotFound();
        }

        // ✅ ✅ ADMIN — GET ALL PROMO CODES (YOU REQUESTED THIS)
        [HttpGet("all")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var promos = await _promoService.GetAllAsync(ct);

            var dto = promos.Select(p => new PromoReadDto
            {
                Id = p.Id,
                Code = p.Code,
                DiscountAmount = p.DiscountAmount,
                IsActive = p.IsActive,
                MinOrderAmount = p.MinOrderAmount,
                StartDateUtc = p.StartDateUtc,
                EndDateUtc = p.EndDateUtc
            });

            return Ok(dto);
        }

        // ✅ USER — APPLY PROMO
        [HttpPost("apply")]
        [Authorize(Policy = "UserOnly")]
        public async Task<IActionResult> ApplyPromo(ApplyPromoDto dto, CancellationToken ct)
        {
            var promo = await _promoService.GetValidPromoAsync(dto.PromoCode.ToUpper(), dto.CartTotal, ct);

            if (promo == null)
                return BadRequest(new { message = "Invalid or expired promo code." });

            return Ok(new PromoReadDto
            {
                Id = promo.Id,
                Code = promo.Code,
                DiscountAmount = promo.DiscountAmount,
                IsActive = promo.IsActive,
                MinOrderAmount = promo.MinOrderAmount,
                StartDateUtc = promo.StartDateUtc,
                EndDateUtc = promo.EndDateUtc
            });
        }
    }
}