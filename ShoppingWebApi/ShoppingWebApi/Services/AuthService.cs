using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Auth;
using ShoppingWebApi.Services.Security;

namespace ShoppingWebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, UserDetails> _detailsRepo;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IRepository<int, User> userRepo,
            IRepository<int, UserDetails> detailsRepo,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _detailsRepo = detailsRepo;
            _tokenService = tokenService;
            _logger = logger;
        }

        // --------------------------------------------------------------------
        // REGISTER (REPO + IQUERY)
        // --------------------------------------------------------------------
        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
        {
            // Check email exists
            var exists = await _userRepo.GetQueryable()
                .AsNoTracking()
                .AnyAsync(u => u.Email == dto.Email, ct);

            if (exists)
                throw new ConflictException("Email already registered.");

            var user = new User
            {
                Email = dto.Email.Trim(),
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role!.Trim(),
                CreatedUtc = DateTime.UtcNow
            };

            var saved = await _userRepo.Add(user);

            // Create UserDetails only if provided
            if (!string.IsNullOrWhiteSpace(dto.FirstName)
                || !string.IsNullOrWhiteSpace(dto.LastName)
                || !string.IsNullOrWhiteSpace(dto.Phone))
            {
                var details = new UserDetails
                {
                    UserId = saved!.Id,
                    FirstName = dto.FirstName ?? "",
                    LastName = dto.LastName ?? "",
                    Phone = dto.Phone
                };
                await _detailsRepo.Add(details);
            }

            var claims = BuildClaims(saved!);
            var token = _tokenService.CreateAccessToken(claims, out var exp);

            _logger.LogInformation("User registered: {Email}", dto.Email);

            return new AuthResponseDto
            {
                //UserId = saved!.Id,
                //Email = saved.Email,
                //Role = saved.Role ?? "User",
                AccessToken = token,
                //ExpiresAtUtc = exp
            };
        }

        // --------------------------------------------------------------------
        // LOGIN (REPO + IQUERY)
        // --------------------------------------------------------------------
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
        {
            var user = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

            if (user == null)
                throw new UnauthorizedAppException("Invalid credentials.");

            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAppException("Invalid credentials.");

            var claims = BuildClaims(user);
            var token = _tokenService.CreateAccessToken(claims, out var exp);

            _logger.LogInformation("User login successful: {Email}", dto.Email);

            return new AuthResponseDto
            {
               
                AccessToken = token,
            };
        }

        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------
        private static Claim[] BuildClaims(User user)
        {
            return new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("userId", user.Id.ToString())
            };
        }
    }
}