using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Common;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Users;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service) => _service = service;

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("paged")]
        public async Task<IActionResult> GetPaged(
            [FromBody] UserPagedRequestDto request, CancellationToken ct = default)
        {
            var res = await _service.GetPagedAsync(
                request.Email, request.Role, request.Name,
                request.SortBy, request.Desc, request.Page, request.Size, ct);
            return Ok(res);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string role, CancellationToken ct)
        {
            try
            {
                await _service.UpdateRoleAsync(id, role, ct);
                return Ok(new { message = "Role Updated Successfully" });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            var dto = await _service.GetProfileAsync(userId.Value, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {
                var res = await _service.UpdateProfileAsync(userId.Value, dto, ct);
                return Ok(res);
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [Authorize]
        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            dto.UserId = userId.Value;
            try
            {
                await _service.ChangePasswordAsync(dto, ct);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex) when (ex is NotFoundException or BusinessValidationException or UnauthorizedAppException)
            { return BadRequest(new { message = ex.Message }); }
        }
    }
}
