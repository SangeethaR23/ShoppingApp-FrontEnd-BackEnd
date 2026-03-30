using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Auth;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly Mock<IRepository<int, UserDetails>> _detailsRepoMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IRepository<int, User>>();
            _detailsRepoMock = new Mock<IRepository<int, UserDetails>>();
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            var exp = DateTime.UtcNow.AddHours(1);
            _tokenServiceMock
                .Setup(t => t.CreateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), out exp))
                .Returns("dummy-token");
        }

        // Creates an isolated in-memory DbContext seeded with the given users
        private AppDbContext CreateCtx(params User[] users)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(opts);
            ctx.Users.AddRange(users);
            ctx.SaveChanges();
            return ctx;
        }

        private AuthService BuildSut(AppDbContext ctx)
        {
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
            return new AuthService(
                _userRepoMock.Object,
                _detailsRepoMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object);
        }

        // ──────────────────────────────────────────────
        // REGISTER
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Register_Success_ReturnsToken()
        {
            using var ctx = CreateCtx(); // empty — no existing users
            var sut = BuildSut(ctx);

            var savedUser = new User { Id = 1, Email = "a@b.com", Role = "User" };
            _userRepoMock.Setup(r => r.Add(It.IsAny<User>())).ReturnsAsync(savedUser);
            _detailsRepoMock.Setup(r => r.Add(It.IsAny<UserDetails>())).ReturnsAsync(new UserDetails());

            var result = await sut.RegisterAsync(new RegisterRequestDto
            {
                Email = "a@b.com",
                Password = "Password1!",
                FirstName = "Test",
                LastName = "User"
            });

            Assert.NotNull(result);
            Assert.Equal("dummy-token", result.AccessToken);
            _userRepoMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task Register_WithoutNamePhone_DoesNotCreateDetails()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            var savedUser = new User { Id = 1, Email = "a@b.com", Role = "User" };
            _userRepoMock.Setup(r => r.Add(It.IsAny<User>())).ReturnsAsync(savedUser);

            await sut.RegisterAsync(new RegisterRequestDto { Email = "a@b.com", Password = "Password1!" });

            _detailsRepoMock.Verify(r => r.Add(It.IsAny<UserDetails>()), Times.Never);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsConflictException()
        {
            using var ctx = CreateCtx(new User { Id = 1, Email = "dup@b.com", PasswordHash = "x" });
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<ConflictException>(() =>
                sut.RegisterAsync(new RegisterRequestDto { Email = "dup@b.com", Password = "Password1!" }));
        }

        [Fact]
        public async Task Register_EmptyRole_DefaultsToUser()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            User? captured = null;
            _userRepoMock.Setup(r => r.Add(It.IsAny<User>()))
                         .Callback<User>(u => captured = u)
                         .ReturnsAsync((User u) => u);

            await sut.RegisterAsync(new RegisterRequestDto { Email = "x@x.com", Password = "Pass1!", Role = "" });

            Assert.Equal("User", captured!.Role);
        }

        [Fact]
        public async Task Register_CustomRole_IsUsed()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            User? captured = null;
            _userRepoMock.Setup(r => r.Add(It.IsAny<User>()))
                         .Callback<User>(u => captured = u)
                         .ReturnsAsync((User u) => u);

            await sut.RegisterAsync(new RegisterRequestDto { Email = "admin@x.com", Password = "Pass1!", Role = "Admin" });

            Assert.Equal("Admin", captured!.Role);
        }

        // ──────────────────────────────────────────────
        // LOGIN
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Login_Success_ReturnsToken()
        {
            var hash = ShoppingWebApi.Services.Security.PasswordHasher.Hash("correct");
            using var ctx = CreateCtx(new User { Id = 1, Email = "u@b.com", PasswordHash = hash, Role = "User" });
            var sut = BuildSut(ctx);

            var result = await sut.LoginAsync(new LoginRequestDto { Email = "u@b.com", Password = "correct" });

            Assert.Equal("dummy-token", result.AccessToken);
        }

        [Fact]
        public async Task Login_UserNotFound_ThrowsUnauthorized()
        {
            using var ctx = CreateCtx(); // empty
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
                sut.LoginAsync(new LoginRequestDto { Email = "no@b.com", Password = "pw" }));
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsUnauthorized()
        {
            var hash = ShoppingWebApi.Services.Security.PasswordHasher.Hash("correct");
            using var ctx = CreateCtx(new User { Id = 1, Email = "u@b.com", PasswordHash = hash, Role = "User" });
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
                sut.LoginAsync(new LoginRequestDto { Email = "u@b.com", Password = "wrong" }));
        }
    }
}
