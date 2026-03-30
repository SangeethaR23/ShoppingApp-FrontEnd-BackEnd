using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;

namespace ShoppingApp.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _svcMock = new();
    private readonly ProductsController _sut;

    private static ProductReadDto FakeProduct(int id = 1) => new()
    {
        Id = id, Name = "Test Product", SKU = "SKU-001", Price = 100, CategoryId = 1, IsActive = true
    };

    private static PagedResult<ProductReadDto> FakePage(int count = 1) => new()
    {
        Items = Enumerable.Range(1, count).Select(FakeProduct).ToList(),
        TotalCount = count, PageNumber = 1, PageSize = 10
    };

    public ProductsControllerTests() => _sut = new ProductsController(_svcMock.Object);

    // helper: unwrap ActionResult<T> to IActionResult
    private static IActionResult Unwrap<T>(ActionResult<T> ar) => ar.Result ?? new OkObjectResult(ar.Value);

    // ── GetPaged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200WithItems()
    {
        _svcMock.Setup(s => s.GetAllAsync(1, 10, null, null, default)).ReturnsAsync(FakePage(3));
        var req = new PagedRequestDto { Page = 1, Size = 10 };

        var ar = await _sut.GetPaged(req, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetPaged_EmptyList_Returns200()
    {
        _svcMock.Setup(s => s.GetAllAsync(1, 10, null, null, default))
            .ReturnsAsync(new PagedResult<ProductReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 });
        var req = new PagedRequestDto { Page = 1, Size = 10 };

        var ar = await _sut.GetPaged(req, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_Returns200WithResults()
    {
        var query = new ProductQuery { Page = 1, Size = 10 };
        _svcMock.Setup(s => s.SearchAsync(query, default)).ReturnsAsync(FakePage(2));

        var ar = await _sut.Search(query, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeProduct());

        var ar = await _sut.GetById(1, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        Assert.IsType<ProductReadDto>(result.Value);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((ProductReadDto?)null);

        var ar = await _sut.GetById(99, default);

        Assert.IsType<NotFoundObjectResult>(ar.Result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = new ProductCreateDto { Name = "P", SKU = "S1", Price = 10, CategoryId = 1 };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(FakeProduct());

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Create_CategoryNotFound_Returns400()
    {
        var dto = new ProductCreateDto { Name = "P", SKU = "S1", Price = 10, CategoryId = 99 };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ThrowsAsync(new NotFoundException("Category not found"));

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSku_Returns400()
    {
        var dto = new ProductCreateDto { Name = "P", SKU = "DUP", Price = 10, CategoryId = 1 };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ThrowsAsync(new ConflictException("SKU exists"));

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Create_BusinessValidationFails_Returns400()
    {
        var dto = new ProductCreateDto { Name = "P", SKU = "S1", Price = -1, CategoryId = 1 };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ThrowsAsync(new BusinessValidationException("Invalid"));

        var ar = await _sut.Create(dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidDto_Returns200()
    {
        var dto = new ProductUpdateDto { Id = 1, Name = "Updated", SKU = "S1", Price = 20, CategoryId = 1 };
        _svcMock.Setup(s => s.UpdateAsync(1, dto, default)).ReturnsAsync(FakeProduct());

        var ar = await _sut.Update(1, dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Update_IdMismatch_Returns400()
    {
        var dto = new ProductUpdateDto { Id = 2, Name = "X", SKU = "S1", Price = 10, CategoryId = 1 };
        _svcMock.Setup(s => s.UpdateAsync(1, dto, default)).ThrowsAsync(new BusinessValidationException("Mismatch"));

        var ar = await _sut.Update(1, dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Update_ProductNotFound_Returns400()
    {
        var dto = new ProductUpdateDto { Id = 99, Name = "X", SKU = "S1", Price = 10, CategoryId = 1 };
        _svcMock.Setup(s => s.UpdateAsync(99, dto, default)).ThrowsAsync(new NotFoundException("Not found"));

        var ar = await _sut.Update(99, dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingProduct_Returns200()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, default)).ReturnsAsync(true);

        var result = await _sut.Delete(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFound_Returns400()
    {
        _svcMock.Setup(s => s.DeleteAsync(99, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.Delete(99, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_ReferencedByOrders_Returns400()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, default)).ThrowsAsync(new ConflictException("Referenced"));

        var result = await _sut.Delete(1, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── AddImage ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddImage_ValidRequest_Returns200()
    {
        var dto = new ProductImageCreateDto { Url = "http://img.com/1.jpg" };
        _svcMock.Setup(s => s.AddImageAsync(1, dto, default)).Returns(Task.CompletedTask);

        var result = await _sut.AddImage(1, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task AddImage_ProductNotFound_Returns400()
    {
        var dto = new ProductImageCreateDto { Url = "http://img.com/1.jpg" };
        _svcMock.Setup(s => s.AddImageAsync(99, dto, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.AddImage(99, dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── RemoveImage ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveImage_ValidRequest_Returns200()
    {
        _svcMock.Setup(s => s.RemoveImageAsync(1, 5, default)).ReturnsAsync(true);

        var result = await _sut.RemoveImage(1, 5, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task RemoveImage_NotFound_Returns400()
    {
        _svcMock.Setup(s => s.RemoveImageAsync(1, 99, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.RemoveImage(1, 99, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── SetActive ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetActive_True_Returns200()
    {
        _svcMock.Setup(s => s.SetActiveAsync(1, true, default)).Returns(Task.CompletedTask);

        var result = await _sut.SetActive(1, true, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task SetActive_False_Returns200()
    {
        _svcMock.Setup(s => s.SetActiveAsync(1, false, default)).Returns(Task.CompletedTask);

        var result = await _sut.SetActive(1, false, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task SetActive_NotFound_Returns400()
    {
        _svcMock.Setup(s => s.SetActiveAsync(99, true, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.SetActive(99, true, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── GetReviews ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReviews_ValidProduct_Returns200()
    {
        var page = new PagedResult<ReviewReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetReviewsByProductIdAsync(1, 1, 10, null, "newest", "desc", default)).ReturnsAsync(page);

        var ar = await _sut.GetReviewsByProductId(1, 1, 10, null, "newest", "desc", default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetReviews_ProductNotFound_Returns400()
    {
        _svcMock.Setup(s => s.GetReviewsByProductIdAsync(99, 1, 10, null, "newest", "desc", default))
            .ThrowsAsync(new NotFoundException("Not found"));

        var ar = await _sut.GetReviewsByProductId(99, 1, 10, null, "newest", "desc", default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }
}
