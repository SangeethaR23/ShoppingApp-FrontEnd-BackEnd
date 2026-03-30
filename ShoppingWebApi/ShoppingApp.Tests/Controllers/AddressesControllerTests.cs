using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Address;
using ShoppingWebApi.Models.DTOs.Addresses;
using ShoppingWebApi.Models.DTOs.Common;

namespace ShoppingApp.Tests.Controllers;

public class AddressesControllerTests
{
    private readonly Mock<IAddressService> _svcMock = new();
    private readonly AddressesController _sut;

    private static AddressReadDto FakeAddress(int id = 1, int userId = 1)
    {
        // UserId and CreatedUtc have internal setters — use reflection to set them in tests
        var dto = new AddressReadDto
        {
            Id = id, FullName = "John Doe",
            Line1 = "123 Main St", City = "Chennai", State = "TN",
            PostalCode = "600001", Country = "India"
        };
        typeof(AddressReadDto).GetProperty("UserId")!.SetValue(dto, userId);
        typeof(AddressReadDto).GetProperty("CreatedUtc")!.SetValue(dto, DateTime.UtcNow);
        return dto;
    }

    private static PagedResult<AddressReadDto> FakePage() => new()
    {
        Items = [FakeAddress()], TotalCount = 1, PageNumber = 1, PageSize = 10
    };

    public AddressesControllerTests()
    {
        _sut = new AddressesController(_svcMock.Object);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = new AddressCreateDto { FullName = "John", Line1 = "123 St", City = "City", State = "ST", PostalCode = "12345" };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<AddressCreateDto>(), default)).ReturnsAsync(FakeAddress());

        var result = await _sut.Create(dto, default) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Create_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new AddressCreateDto { FullName = "John", Line1 = "123 St", City = "City", State = "ST", PostalCode = "12345" };

        var result = await _sut.Create(dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Create_SetsUserIdFromClaims()
    {
        ControllerTestHelper.SetUser(_sut, userId: 5);
        var dto = new AddressCreateDto { FullName = "John", Line1 = "123 St", City = "City", State = "ST", PostalCode = "12345" };
        AddressCreateDto? captured = null;
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<AddressCreateDto>(), default))
            .Callback<AddressCreateDto, CancellationToken>((d, _) => captured = d)
            .ReturnsAsync(FakeAddress(userId: 5));

        await _sut.Create(dto, default);

        Assert.Equal(5, captured!.UserId);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OwnAddress_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, userId: 1);
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeAddress(1, 1));

        var result = await _sut.GetById(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((AddressReadDto?)null);

        var result = await _sut.GetById(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_OtherUsersAddress_Returns403()
    {
        ControllerTestHelper.SetUser(_sut, userId: 1, role: "User");
        // Address belongs to userId=2, but requester is userId=1
        _svcMock.Setup(s => s.GetByIdAsync(5, default)).ReturnsAsync(FakeAddress(5, userId: 2));

        var result = await _sut.GetById(5, default);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetById_AdminCanAccessAnyAddress_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, userId: 1, role: "Admin");
        _svcMock.Setup(s => s.GetByIdAsync(5, default)).ReturnsAsync(FakeAddress(5, userId: 2));

        var result = await _sut.GetById(5, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetById_NoUserClaim_Returns403()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeAddress(1, 1));

        var result = await _sut.GetById(1, default);

        // No userId claim → Forbid (address.UserId != null)
        Assert.IsType<ForbidResult>(result);
    }

    // ── GetMine ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMine_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetByUserAsync(1, 1, 10, default)).ReturnsAsync(FakePage());

        var result = await _sut.GetMine(1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMine_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.GetMine(1, 10, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMine_EmptyList_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetByUserAsync(1, 1, 10, default))
            .ReturnsAsync(new PagedResult<AddressReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 });

        var result = await _sut.GetMine(1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── GetByUser (Admin) ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUser_Admin_Returns200()
    {
        _svcMock.Setup(s => s.GetByUserAsync(5, 1, 10, default)).ReturnsAsync(FakePage());

        var result = await _sut.GetByUser(5, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new AddressUpdateDto { FullName = "Jane", Line1 = "456 St", City = "City", State = "ST", PostalCode = "12345" };
        _svcMock.Setup(s => s.UpdateAsync(1, 1, dto, default)).ReturnsAsync(true);

        var result = await _sut.Update(1, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Update_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new AddressUpdateDto { FullName = "Jane", Line1 = "456 St", City = "City", State = "ST", PostalCode = "12345" };

        var result = await _sut.Update(1, dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_OwnAddress_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.DeleteAsync(1, 1, default)).ReturnsAsync(true);

        var result = await _sut.Delete(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.DeleteAsync(99, 1, default)).ReturnsAsync(false);

        var result = await _sut.Delete(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.Delete(1, default);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
