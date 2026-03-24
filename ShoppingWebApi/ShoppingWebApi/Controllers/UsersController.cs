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

        public UsersController(IUserService service)
        {
            _service = service;
        }

        // ======================================================
        // ADMIN: POST PAGED USERS
        // ======================================================
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("paged")]
        [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromBody] UserPagedRequestDto request,
            CancellationToken ct = default)
        {
            var res = await _service.GetPagedAsync(
                request.Email,
                request.Role,
                request.Name,
                request.SortBy,
                request.Desc,
                request.Page,
                request.Size,
                ct);

            return Ok(res);
        }

        // ======================================================
        // ADMIN: GET USER BY ID
        // ======================================================
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserProfileReadDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        // ======================================================
        // ADMIN: UPDATE ROLE
        // ======================================================
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string role, CancellationToken ct)
        {
            try
            {
                await _service.UpdateRoleAsync(id, role, ct);
                return Ok(new { message = "Role Updated Successfully" });
            }
            catch (BusinessValidationException)
            {
                return BadRequest(new { message = "Role must be 'Admin' or 'User'" });
            }
            catch (NotFoundException)
            {
                return BadRequest(new { message = "User not found" });
            }

        }

        // ======================================================
        // USER: GET MY PROFILE
        // ======================================================
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var dto = await _service.GetProfileAsync(userId.Value, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        // ======================================================
        // USER: UPDATE MY PROFILE
        // ======================================================
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
            catch (NotFoundException)
            {
                return BadRequest(new { message = "User Not Found" });
            }
        }

        // ======================================================
        // USER: CHANGE PASSWORD
        // ======================================================
        [Authorize]
        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();
            try
            {
                dto.UserId = userId.Value; // override for safety

                await _service.ChangePasswordAsync(dto, ct);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (BusinessValidationException)
            {
                return BadRequest(new { message = "New password must be at least 6 characters." });
            }
            catch (NotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch (UnauthorizedAppException)
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

        }
    }
}