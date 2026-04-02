using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Common;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Reviews;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service) => _service = service;

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReviewCreateDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            dto.UserId = userId.Value;
            try
            {
                var res = await _service.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetMineForProduct), new { productId = res.ProductId }, res);
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(
            [FromRoute] int productId, [FromQuery] int page = 1,
            [FromQuery] int size = 10, CancellationToken ct = default)
        {
            var res = await _service.GetByProductAsync(productId, page, size, ct);
            return Ok(res);
        }

        [Authorize]
        [HttpGet("product/{productId:int}/mine")]
        public async Task<IActionResult> GetMineForProduct([FromRoute] int productId, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            var dto = await _service.GetAsync(productId, userId.Value, ct);
            return Ok(dto);
        }

        [Authorize]
        [HttpPut("product/{productId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int productId, [FromBody] ReviewUpdateDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {
                await _service.UpdateAsync(productId, userId.Value, dto, ct);
                return Ok(new { message = "Review Updated Successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpDelete("product/{productId:int}")]
        public async Task<IActionResult> Delete([FromRoute] int productId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            var ok = await _service.DeleteAsync(productId, userId.Value, ct);
            return ok ? Ok(new { message = "Review Deleted Successfully" }) : NotFound();
        }
    }
}
