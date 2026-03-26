using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletSvc;
        private readonly AppDbContext _db;

        public WalletController(IWalletService walletSvc, AppDbContext db)
        {
            _walletSvc = walletSvc;
            _db = db;
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")!.Value);

        // GET /api/wallet/me  — returns balance
        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet(CancellationToken ct)
        {
            var userId = GetUserId();
            var wallet = await _walletSvc.GetAsync(userId, ct);

            if (wallet == null)
            {
                // Return a zero-balance wallet DTO rather than 404
                return Ok(new { id = 0, userId, balance = 0m });
            }

            return Ok(new { id = wallet.Id, userId = wallet.UserId, balance = wallet.Balance });
        }

        // GET /api/wallet/me/transactions?page=1&size=10
        [HttpGet("me/transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            CancellationToken ct = default)
        {
            var userId = GetUserId();

            var wallet = await _db.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);

            if (wallet == null)
                return Ok(new { items = Array.Empty<object>(), totalCount = 0, pageNumber = page, pageSize = size });

            var query = _db.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedUtc);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(t => new
                {
                    t.Id,
                    t.WalletId,
                    t.UserId,
                    t.Amount,
                    Type = t.Type.ToString(),
                    t.Reference,
                    t.Remarks,
                    t.CreatedUtc
                })
                .ToListAsync(ct);

            return Ok(new { items, totalCount = total, pageNumber = page, pageSize = size });
        }
    }
}
