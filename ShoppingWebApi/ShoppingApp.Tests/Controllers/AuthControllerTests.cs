using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Auth;

namespace ShoppingApp.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authMock = new();
    private readonly AuthController _sut;

    private static readonly AuthResponseDto FakeResponse = new() { AccessToken = "fake.token" };

    public AuthControllerTests() => _sut = new AuthController(_authMock.Object);

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidDto_Returns201WithToken()
    {
        var dto = new RegisterRequestDto { Email = "a@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.RegisterAsync(dto, default)).ReturnsAsync(FakeResponse);

        var result = await _sut.Register(dto, default) as CreatedResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        Assert.Equal(FakeResponse, result.Value);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflictException()
    {
        var dto = new RegisterRequestDto { Email = "dup@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.RegisterAsync(dto, default)).ThrowsAsync(new ConflictException("Email already registered."));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.Register(dto, default));
    }

    [Fact]
    public async Task Register_ServiceThrows_PropagatesException()
    {
        var dto = new RegisterRequestDto { Email = "x@x.com", Password = "p" };
        _authMock.Setup(s => s.RegisterAsync(dto, default)).ThrowsAsync(new Exception("unexpected"));

        await Assert.ThrowsAsync<Exception>(() => _sut.Register(dto, default));
    }

    [Fact]
    public async Task Register_CallsServiceOnce()
    {
        var dto = new RegisterRequestDto { Email = "once@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.RegisterAsync(dto, default)).ReturnsAsync(FakeResponse);

        await _sut.Register(dto, default);

        _authMock.Verify(s => s.RegisterAsync(dto, default), Times.Once);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var dto = new LoginRequestDto { Email = "a@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.LoginAsync(dto, default)).ReturnsAsync(FakeResponse);

        var result = await _sut.Login(dto, default) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        Assert.Equal(FakeResponse, result.Value);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorizedAppException()
    {
        var dto = new LoginRequestDto { Email = "a@b.com", Password = "wrong" };
        _authMock.Setup(s => s.LoginAsync(dto, default)).ThrowsAsync(new UnauthorizedAppException("Invalid credentials."));

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => _sut.Login(dto, default));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedAppException()
    {
        var dto = new LoginRequestDto { Email = "ghost@b.com", Password = "p" };
        _authMock.Setup(s => s.LoginAsync(dto, default)).ThrowsAsync(new UnauthorizedAppException("Invalid credentials."));

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => _sut.Login(dto, default));
    }

    [Fact]
    public async Task Login_CallsServiceOnce()
    {
        var dto = new LoginRequestDto { Email = "a@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.LoginAsync(dto, default)).ReturnsAsync(FakeResponse);

        await _sut.Login(dto, default);

        _authMock.Verify(s => s.LoginAsync(dto, default), Times.Once);
    }

    [Fact]
    public async Task Login_ReturnsTokenInBody()
    {
        var dto = new LoginRequestDto { Email = "a@b.com", Password = "Pass@1" };
        _authMock.Setup(s => s.LoginAsync(dto, default)).ReturnsAsync(FakeResponse);

        var result = await _sut.Login(dto, default) as OkObjectResult;

        var response = result!.Value as AuthResponseDto;
        Assert.Equal("fake.token", response!.AccessToken);
    }
}
