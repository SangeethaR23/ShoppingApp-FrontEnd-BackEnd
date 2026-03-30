using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Users;

namespace ShoppingApp.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _svcMock = new();
    private readonly UsersController _sut;

    private static UserProfileReadDto FakeProfile(int id = 1) => new()
    {
        Id = id, Email = "user@test.com", Role = "User",
        FirstName = "John", LastName = "Doe", CreatedUtc = DateTime.UtcNow
    };

    public UsersControllerTests()
    {
        _sut = new UsersController(_svcMock.Object);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    // ── Admin: GetPaged ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200()
    {
        var page = new PagedResult<UserListItemDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetPagedAsync(null, null, null, null, false, 1, 10, default)).ReturnsAsync(page);
        var req = new UserPagedRequestDto { Page = 1, Size = 10 };

        var result = await _sut.GetPaged(req, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Admin: GetById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingUser_Returns200()
    {
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeProfile());

        var result = await _sut.GetById(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((UserProfileReadDto?)null);

        var result = await _sut.GetById(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Admin: UpdateRole ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_ValidRole_Returns200()
    {
        _svcMock.Setup(s => s.UpdateRoleAsync(1, "Admin", default)).ReturnsAsync(true);

        var result = await _sut.UpdateRole(1, "Admin", default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_InvalidRole_Returns400()
    {
        _svcMock.Setup(s => s.UpdateRoleAsync(1, "SuperAdmin", default))
            .ThrowsAsync(new BusinessValidationException("Invalid role"));

        var result = await _sut.UpdateRole(1, "SuperAdmin", default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_UserNotFound_Returns400()
    {
        _svcMock.Setup(s => s.UpdateRoleAsync(99, "User", default))
            .ThrowsAsync(new NotFoundException("User not found"));

        var result = await _sut.UpdateRole(99, "User", default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── GetMe ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_AuthenticatedUser_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetProfileAsync(1, default)).ReturnsAsync(FakeProfile());

        var result = await _sut.GetMe(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.GetMe(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMe_ProfileNotFound_Returns404()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetProfileAsync(1, default)).ReturnsAsync((UserProfileReadDto?)null);

        var result = await _sut.GetMe(default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── UpdateMe ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMe_ValidDto_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new UpdateUserProfileDto { FirstName = "Jane" };
        _svcMock.Setup(s => s.UpdateProfileAsync(1, dto, default)).ReturnsAsync(FakeProfile());

        var result = await _sut.UpdateMe(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new UpdateUserProfileDto { FirstName = "Jane" };

        var result = await _sut.UpdateMe(dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateMe_UserNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new UpdateUserProfileDto { FirstName = "Jane" };
        _svcMock.Setup(s => s.UpdateProfileAsync(1, dto, default))
            .ThrowsAsync(new NotFoundException("User not found"));

        var result = await _sut.UpdateMe(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Old@1", NewPassword = "New@1" };
        _svcMock.Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequestDto>(), default)).ReturnsAsync(true);

        var result = await _sut.ChangePassword(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Old@1", NewPassword = "New@1" };

        var result = await _sut.ChangePassword(dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Wrong", NewPassword = "New@1" };
        _svcMock.Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequestDto>(), default))
            .ThrowsAsync(new UnauthorizedAppException("Current password is incorrect."));

        var result = await _sut.ChangePassword(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Old@1", NewPassword = "123" };
        _svcMock.Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequestDto>(), default))
            .ThrowsAsync(new BusinessValidationException("Too short"));

        var result = await _sut.ChangePassword(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Old@1", NewPassword = "New@1" };
        _svcMock.Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequestDto>(), default))
            .ThrowsAsync(new NotFoundException("User not found"));

        var result = await _sut.ChangePassword(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_SetsUserIdFromClaims()
    {
        ControllerTestHelper.SetUser(_sut, userId: 5);
        var dto = new ChangePasswordRequestDto { CurrentPassword = "Old@1", NewPassword = "New@1" };
        ChangePasswordRequestDto? captured = null;
        _svcMock.Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequestDto>(), default))
            .Callback<ChangePasswordRequestDto, CancellationToken>((d, _) => captured = d)
            .ReturnsAsync(true);

        await _sut.ChangePassword(dto, default);

        Assert.Equal(5, captured!.UserId);
    }
}
