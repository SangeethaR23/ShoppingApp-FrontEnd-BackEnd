using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Common;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service) => _service = service;

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Place([FromBody] PlaceOrderRequestDto request, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            request.UserId = userId.Value;
            try
            {
                var res = await _service.PlaceOrderAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        [Authorize]
        [HttpPost("mine")]
        public async Task<IActionResult> GetMine(
            [FromBody] OrderPagedRequestDto request, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            var result = await _service.GetUserOrdersAsync(
                userId.Value, request.Page, request.Size, request.SortBy,
                request.Desc, request.Status, request.From, request.To, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id, [FromQuery] string? reason = null, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            var isAdmin = User.IsInRole("Admin");
            var result = await _service.CancelOrderAsync(id, userId.Value, isAdmin, reason, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:int}/return")]
        public async Task<IActionResult> Return(
            int id, [FromQuery] string? reason = null, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {
                var dto = new ReturnRequestCreateDto { OrderId = id, Reason = reason ?? "Return requested by user" };
                await _service.RequestReturnAsync(userId.Value, dto, ct);
                return Ok(new { message = "Return request submitted successfully." });
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) when (ex is BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("paged")]
        public async Task<IActionResult> GetPaged(
            [FromBody] OrderPagedRequestDto req, CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(
                req.Status, req.From, req.To, req.UserId,
                req.Page, req.Size, req.SortBy, req.Desc, ct);
            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] UpdateOrderStatusRequset request, CancellationToken ct)
        {
            var ok = await _service.UpdateStatusAsync(id, request.Status, ct);
            if (!ok) return BadRequest("Order not found or invalid status.");
            return Ok(new { message = "Order status updated Successfully" });
        }
    }
}
