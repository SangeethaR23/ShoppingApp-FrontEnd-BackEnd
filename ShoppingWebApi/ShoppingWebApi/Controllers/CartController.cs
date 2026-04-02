using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Common;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Cart;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service) => _service = service;

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<CartReadDto>> GetMyCart(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetByUserIdAsync(userId.Value, ct));
        }

        [Authorize]
        [HttpPost("items")]
        public async Task<ActionResult<CartReadDto>> AddItemMe([FromBody] CartAddItemDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try { return Ok(await _service.AddItemAsync(userId.Value, dto, ct)); }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpPut("items")]
        public async Task<ActionResult<CartReadDto>> UpdateItemMe([FromBody] CartUpdateItemDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try { return Ok(await _service.UpdateItemAsync(userId.Value, dto, ct)); }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpDelete("items/{productId:int}")]
        public async Task<IActionResult> RemoveItemMe([FromRoute] int productId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {
                await _service.RemoveItemAsync(userId.Value, productId, ct);
                return Ok(new { message = "Item removed successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpDelete("items")]
        public async Task<IActionResult> ClearMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            await _service.ClearAsync(userId.Value, ct);
            return Ok(new { message = "Cart cleared successfully" });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("by-user/{userId:int}")]
        public async Task<ActionResult<CartReadDto>> GetByUserId(int userId, CancellationToken ct)
            => Ok(await _service.GetByUserIdAsync(userId, ct));

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("by-user/{userId:int}/items")]
        public async Task<ActionResult<CartReadDto>> AddItem(int userId, [FromBody] CartAddItemDto dto, CancellationToken ct)
            => Ok(await _service.AddItemAsync(userId, dto, ct));

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("by-user/{userId:int}/items")]
        public async Task<ActionResult<CartReadDto>> UpdateItem(int userId, [FromBody] CartUpdateItemDto dto, CancellationToken ct)
            => Ok(await _service.UpdateItemAsync(userId, dto, ct));

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("by-user/{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> RemoveItem(int userId, int productId, CancellationToken ct)
        {
            await _service.RemoveItemAsync(userId, productId, ct);
            return Ok(new { message = "Product removed successfully" });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("by-user/{userId:int}/items")]
        public async Task<IActionResult> Clear(int userId, CancellationToken ct)
        {
            await _service.ClearAsync(userId, ct);
            return Ok(new { message = "Cleared successfully" });
        }
    }
}
