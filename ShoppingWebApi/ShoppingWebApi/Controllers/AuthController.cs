using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Auth;

namespace ShoppingWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            var res = await _auth.RegisterAsync(dto, ct);
            return Created(string.Empty, res);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        {
            var res = await _auth.LoginAsync(dto, ct);
            return Ok(res);
        }
    }
}
