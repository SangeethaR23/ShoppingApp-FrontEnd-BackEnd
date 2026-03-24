using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Models.DTOs.Common;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        // ================================
        // POST: api/categories/paged
        // SERVER-SIDE PAGING + SORTING
        // ================================
        [HttpPost("paged")]
        [ProducesResponseType(typeof(PagedResult<CategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CategoryReadDto>>> GetPaged(
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

        // GET: api/categories/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryReadDto>> GetById(int id, CancellationToken ct = default)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // POST: api/categories  (Admin only)
        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<ActionResult<CategoryReadDto>> Create(
            [FromBody] CategoryCreateDto dto,
            CancellationToken ct = default)
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/categories/5 (Admin only)
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CategoryReadDto>> Update(
            int id,
            [FromBody] CategoryUpdateDto dto,
            CancellationToken ct = default)
        {
            var updated = await _service.UpdateAsync(id, dto, ct);
            return Ok(new {message="Category Updated Successfully."});
        }

        // DELETE: api/categories/5 (Admin only)
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            await _service.DeleteAsync(id, ct);
            return Ok(new { message = "Category deleted successfully" });
        }
    }
}