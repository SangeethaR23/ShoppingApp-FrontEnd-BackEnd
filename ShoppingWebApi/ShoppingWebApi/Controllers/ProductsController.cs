using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service) => _service = service;

        [HttpPost("paged")]
        public async Task<ActionResult<PagedResult<ProductReadDto>>> GetPaged(
            [FromBody] PagedRequestDto request, CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(request.Page, request.Size, request.SortBy, request.SortDir, ct);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<ProductReadDto>>> Search(
            [FromQuery] ProductQuery query, CancellationToken ct = default)
        {
            var result = await _service.SearchAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductReadDto>> GetById(int id, CancellationToken ct = default)
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (result == null) return NotFound(new { message = "Product not found" });
            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<ActionResult<ProductReadDto>> Create(
            [FromBody] ProductCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var created = await _service.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex) when (ex is NotFoundException or ConflictException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductReadDto>> Update(
            int id, [FromBody] ProductUpdateDto dto, CancellationToken ct = default)
        {
            try
            {
                await _service.UpdateAsync(id, dto, ct);
                return Ok(new { message = "Product updated successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or ConflictException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _service.DeleteAsync(id, ct);
                return Ok(new { message = "Product deleted successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or ConflictException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> AddImage(
            int id, [FromBody] ProductImageCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                await _service.AddImageAsync(id, dto, ct);
                return Ok(new { message = "Image added successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken ct = default)
        {
            try
            {
                await _service.RemoveImageAsync(id, imageId, ct);
                return Ok(new { message = "Image removed successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive, CancellationToken ct = default)
        {
            try
            {
                await _service.SetActiveAsync(id, isActive, ct);
                return Ok(new { message = "Product status changed successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}/reviews")]
        public async Task<ActionResult<PagedResult<ReviewReadDto>>> GetReviewsByProductId(
            int id, [FromQuery] int page = 1, [FromQuery] int size = 10,
            [FromQuery] int? minRating = null, [FromQuery] string? sortBy = "newest",
            [FromQuery] string? sortDir = "desc", CancellationToken ct = default)
        {
            try
            {
                var result = await _service.GetReviewsByProductIdAsync(id, page, size, minRating, sortBy, sortDir, ct);
                return Ok(result);
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
