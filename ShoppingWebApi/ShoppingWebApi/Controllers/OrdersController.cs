using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Common;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        // ============================================
        // PLACE ORDER
        // ============================================
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(PlaceOrderResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Place([FromBody] PlaceOrderRequestDto request, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {

                request.UserId = userId.Value;

                var res = await _service.PlaceOrderAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
            }catch(BusinessValidationException e)
            {
                //return BadRequest(new { message = "Insufficient inventory for product"});
                return BadRequest(e.Message);

            }
        }

        // ============================================
        // GET ORDER BY ID
        // ============================================
        [Authorize]
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(OrderReadDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        // ============================================
        // MY ORDERS (NOW POST + USE OrderPagedRequestDto)
        // ============================================
        [Authorize]
        [HttpPost("mine")]
        [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine(
            [FromBody] OrderPagedRequestDto request,
            CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _service.GetUserOrdersAsync(
                userId.Value,
                request.Page,
                request.Size,
                request.SortBy,
                request.Desc,
                request.Status,
                request.From,
                request.To,
                ct);

            return Ok(result);
        }

        // ============================================
        // CANCEL ORDER
        // ============================================
        [Authorize]
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(CancelOrderResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Cancel(
            int id,
            [FromQuery] string? reason = null,
            CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {

                var isAdmin = User.IsInRole("Admin");

                var result = await _service.CancelOrderAsync(id, userId.Value, isAdmin, reason, ct);
                return Ok(result);
            }catch(ConflictException )
            {
                throw;
            }
        }

        // ============================================
        // RETURN ORDER
        // ============================================
        [Authorize]
        [HttpPost("{id:int}/return")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Return(
            int id,
            [FromQuery] string? reason = null,
            CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var dto = new ReturnRequestCreateDto { OrderId = id, Reason = reason ?? "Return requested by user" };
                await _service.RequestReturnAsync(userId.Value, dto, ct);
                return Ok(new { message = "Return request submitted successfully." });
            }
            catch (NotFoundException e) { return NotFound(new { message = e.Message }); }
            catch (BusinessValidationException e) { return BadRequest(new { message = e.Message }); }
        }

        // ============================================
        // ADMIN — GET ALL (POST PAGED)
        // ============================================
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("paged")]
        [ProducesResponseType(typeof(PagedResult<OrderReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromBody] OrderPagedRequestDto req,
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(
                req.Status,
                req.From,
                req.To,
                req.UserId,
                req.Page,
                req.Size,
                req.SortBy,
                req.Desc,
                ct);

            return Ok(result);
        }

        // ============================================
        // ADMIN — UPDATE ORDER STATUS
        // ============================================
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateOrderStatusRequset request,
            CancellationToken ct)
        {
            var ok = await _service.UpdateStatusAsync(id, request.Status, ct);
            if (!ok) return BadRequest("Order not found or invalid status.");

            return Ok(new { message = "Order status updated Successfully" });
        }
    }
}