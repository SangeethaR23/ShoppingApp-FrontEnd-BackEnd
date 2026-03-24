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

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        // ============================================
        // POST PAGED PRODUCTS (instead of GET)
        // POST: /api/products/paged
        // ============================================
        [HttpPost("paged")]
        [ProducesResponseType(typeof(PagedResult<ProductReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductReadDto>>> GetPaged(
            [FromBody] PagedRequestDto request,
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(
                request.Page,
                request.Size,
                request.SortBy,
                request.SortDir,
                ct);

            return Ok(result);
        }

        // ---------------------------------------
        // SEARCH (public) - GET stays same
        // ---------------------------------------
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<ProductReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductReadDto>>> Search(
            [FromQuery] ProductQuery query,
            CancellationToken ct = default)
        {
            var result = await _service.SearchAsync(query, ct);
            return Ok(result);
        }

        // ---------------------------------------
        // GET PRODUCT BY ID (public)
        // ---------------------------------------
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductReadDto>> GetById(int id, CancellationToken ct = default)
        {

            var result = await _service.GetByIdAsync(id, ct);
            if (result == null) return NotFound(new {message="Product not found"});
            return Ok(result);
        }

        // ---------------------------------------
        // CREATE PRODUCT (Admin only)
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<ActionResult<ProductReadDto>> Create(
            [FromBody] ProductCreateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                var created = await _service.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (NotFoundException)
            {
                return BadRequest(new { message = "Category not Found" });
            }
            catch (ConflictException)
            {
                return BadRequest(new { message = "SKU already exists." });
            }
            catch (BusinessValidationException)
            {
                return BadRequest(new { message = "Failed to create product." });
            }
        
        }

        // ---------------------------------------
        // UPDATE PRODUCT (Admin only)
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductReadDto>> Update(
            int id,
            [FromBody] ProductUpdateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, dto, ct);
                return Ok(new { message = "Product updated successfully" });
            }catch(BusinessValidationException)
            {
                return BadRequest(new { message = "Route id mismatch." });

            }catch(NotFoundException)
            {
                return BadRequest(new { message = "Product or category not found" });
            }
        }

        // ---------------------------------------
        // DELETE PRODUCT (Admin only)
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _service.DeleteAsync(id, ct);
                return Ok(new { message = "Product deleted successfully" });
            }
            catch (NotFoundException)
            {
                return BadRequest(new { message = "Product not found" });
            }
            catch (ConflictException)
            {
                return BadRequest(new { message = "Product referenced by Orders." });
            }
        }

        // ---------------------------------------
        // ADD IMAGE TO PRODUCT
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> AddImage(
            int id,
            [FromBody] ProductImageCreateDto dto,
            CancellationToken ct = default)
        {
            try
            {
                await _service.AddImageAsync(id, dto, ct);
                return Ok(new { message = "Image added successfully" });
            }catch(NotFoundException)
            {
                return BadRequest(new { message = "Product not found." });
            }
        }

        // ---------------------------------------
        // REMOVE IMAGE FROM PRODUCT
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> RemoveImage(
            int id,
            int imageId,
            CancellationToken ct = default)
        {
            try
            {
                await _service.RemoveImageAsync(id, imageId, ct);
                return Ok(new { message = "Image removed successfully" });
            }
            catch(NotFoundException)
            {
                return BadRequest(new { message = "Product image not found" });
            }
        }

        // ---------------------------------------
        // ACTIVATE / DEACTIVATE PRODUCT
        // ---------------------------------------
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> SetActive(
            int id,
            [FromQuery] bool isActive,
            CancellationToken ct = default)
        {
            try
            {
                await _service.SetActiveAsync(id, isActive, ct);
                return Ok(new { message = "Product status changed successfully" });
            }catch(NotFoundException)
            {
                return BadRequest(new { message = "Product not found" });
            }
        }

        // ---------------------------------------
        // GET PRODUCT REVIEWS (public)
        // ---------------------------------------
        [HttpGet("{id:int}/reviews")]
        [ProducesResponseType(typeof(PagedResult<ReviewReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ReviewReadDto>>> GetReviewsByProductId(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? minRating = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] string? sortDir = "desc",
            CancellationToken ct = default)
        {
            try
            {
                var result = await _service.GetReviewsByProductIdAsync(
                    id, page, size, minRating, sortBy, sortDir, ct
                );

                return Ok(result);
            }catch(NotFoundException)
            {
                return BadRequest(new { message = "Product not found" });
            }
        }
    }
}
