using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Models.DTOs.Common;

namespace ShoppingApp.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _svcMock = new();
    private readonly CategoriesController _sut;

    private static CategoryReadDto FakeCategory(int id = 1) => new()
    {
        Id = id, Name = "Electronics", CreatedUtc = DateTime.UtcNow
    };

    private static PagedResult<CategoryReadDto> FakePage(int count = 2) => new()
    {
        Items = Enumerable.Range(1, count).Select(FakeCategory).ToList(),
        TotalCount = count, PageNumber = 1, PageSize = 10
    };

    public CategoriesControllerTests() => _sut = new CategoriesController(_svcMock.Object);

    private static IActionResult Unwrap<T>(ActionResult<T> ar) => ar.Result ?? new OkObjectResult(ar.Value);

    // ── GetPaged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200WithItems()
    {
        _svcMock.Setup(s => s.GetAllAsync(1, 10, null, null, default)).ReturnsAsync(FakePage());
        var req = new PagedRequestDto { Page = 1, Size = 10 };

        var ar = await _sut.GetPaged(req, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetPaged_EmptyList_Returns200()
    {
        _svcMock.Setup(s => s.GetAllAsync(1, 10, null, null, default))
            .ReturnsAsync(new PagedResult<CategoryReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 });
        var req = new PagedRequestDto { Page = 1, Size = 10 };

        var ar = await _sut.GetPaged(req, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeCategory());

        var ar = await _sut.GetById(1, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        Assert.IsType<CategoryReadDto>(result!.Value);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((CategoryReadDto?)null);

        var ar = await _sut.GetById(99, default);

        Assert.IsType<NotFoundResult>(ar.Result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = new CategoryCreateDto { Name = "Books" };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(FakeCategory(2));

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedCategory()
    {
        var dto = new CategoryCreateDto { Name = "Books" };
        var created = FakeCategory(5);
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(created);

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as CreatedAtActionResult;

        Assert.Equal(created, result!.Value);
    }

    [Fact]
    public async Task Create_CallsServiceOnce()
    {
        var dto = new CategoryCreateDto { Name = "Books" };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(FakeCategory());

        await _sut.Create(dto, default);

        _svcMock.Verify(s => s.CreateAsync(dto, default), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidDto_Returns200()
    {
        var dto = new CategoryUpdateDto { Name = "Updated" };
        _svcMock.Setup(s => s.UpdateAsync(1, dto, default)).ReturnsAsync(FakeCategory());

        var ar = await _sut.Update(1, dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Update_CallsServiceOnce()
    {
        var dto = new CategoryUpdateDto { Name = "Updated" };
        _svcMock.Setup(s => s.UpdateAsync(1, dto, default)).ReturnsAsync(FakeCategory());

        await _sut.Update(1, dto, default);

        _svcMock.Verify(s => s.UpdateAsync(1, dto, default), Times.Once);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCategory_Returns200()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, default)).ReturnsAsync(true);

        var result = await _sut.Delete(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_CallsServiceOnce()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, default)).ReturnsAsync(true);

        await _sut.Delete(1, default);

        _svcMock.Verify(s => s.DeleteAsync(1, default), Times.Once);
    }
}
