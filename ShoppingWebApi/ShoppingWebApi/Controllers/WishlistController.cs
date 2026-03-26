using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Wishlist;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize(Policy = "UserOnly")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlist;

        public WishlistController(IWishlistService wishlist)
        {
            _wishlist = wishlist;
        }

        // TOGGLE ADD/REMOVE
        [HttpPost("toggle")]
        public async Task<IActionResult> Toggle(WishlistToggleDto dto, CancellationToken ct)
        {
            var userId = int.Parse(User.FindFirst("userId")!.Value);
            bool added = await _wishlist.ToggleAsync(userId, dto.ProductId, ct);

            return Ok(new
            {
                added,
                message = added ? "Added to wishlist" : "Removed from wishlist"
            });
        }

        // GET WISHLIST
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var userId = int.Parse(User.FindFirst("userId")!.Value);
            var items = await _wishlist.GetAsync(userId, ct);
            return Ok(items);
        }

        // MOVE TO CART
        [HttpPost("move-to-cart/{productId}")]
        public async Task<IActionResult> MoveToCart(int productId, CancellationToken ct)
        {
            var userId = int.Parse(User.FindFirst("userId")!.Value);
            await _wishlist.MoveToCartAsync(userId, productId, ct);
            return Ok(new { message = "Moved to cart" });
        }
    }
}