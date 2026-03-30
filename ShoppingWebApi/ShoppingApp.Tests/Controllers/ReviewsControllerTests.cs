using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Reviews;

namespace ShoppingApp.Tests.Controllers;

public class ReviewsControllerTests
{
    private readonly Mock<IReviewService> _svcMock = new();
    private readonly ReviewsController _sut;

    private static ReviewReadDto FakeReview(int id = 1) => new()
    {
        Id = id, ProductId = 10, UserId = 1, Rating = 5, Comment = "Great!", CreatedUtc = DateTime.UtcNow
    };

    public ReviewsControllerTests()
    {
        _sut = new ReviewsController(_svcMock.Object);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = new ReviewCreateDto { ProductId = 10, Rating = 5 };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<ReviewCreateDto>(), default)).ReturnsAsync(FakeReview());

        var result = await _sut.Create(dto, default) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Create_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new ReviewCreateDto { ProductId = 10, Rating = 5 };

        var result = await _sut.Create(dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Create_ProductNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ReviewCreateDto { ProductId = 99, Rating = 5 };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<ReviewCreateDto>(), default))
            .ThrowsAsync(new NotFoundException("Product not found"));

        var result = await _sut.Create(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRating_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ReviewCreateDto { ProductId = 10, Rating = 0 };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<ReviewCreateDto>(), default))
            .ThrowsAsync(new BusinessValidationException("Rating must be between 1 and 5"));

        var result = await _sut.Create(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Create_SetsUserIdFromClaims()
    {
        ControllerTestHelper.SetUser(_sut, userId: 42);
        var dto = new ReviewCreateDto { ProductId = 10, Rating = 4 };
        ReviewCreateDto? captured = null;
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<ReviewCreateDto>(), default))
            .Callback<ReviewCreateDto, CancellationToken>((d, _) => captured = d)
            .ReturnsAsync(FakeReview());

        await _sut.Create(dto, default);

        Assert.Equal(42, captured!.UserId);
    }

    // ── GetByProduct ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByProduct_Returns200()
    {
        var page = new PagedResult<ReviewReadDto> { Items = [FakeReview()], TotalCount = 1, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetByProductAsync(10, 1, 10, default)).ReturnsAsync(page);

        var result = await _sut.GetByProduct(10, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetByProduct_EmptyList_Returns200()
    {
        var page = new PagedResult<ReviewReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetByProductAsync(10, 1, 10, default)).ReturnsAsync(page);

        var result = await _sut.GetByProduct(10, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── GetMineForProduct ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMineForProduct_WithReview_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetAsync(10, 1, default)).ReturnsAsync(FakeReview());

        var result = await _sut.GetMineForProduct(10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMineForProduct_NoReview_Returns200WithNull()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.GetAsync(10, 1, default)).ReturnsAsync((ReviewReadDto?)null);

        var result = await _sut.GetMineForProduct(10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetMineForProduct_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.GetMineForProduct(10, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidDto_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ReviewUpdateDto { Rating = 4, Comment = "Updated" };
        _svcMock.Setup(s => s.UpdateAsync(10, 1, dto, default)).ReturnsAsync(true);

        var result = await _sut.Update(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Update_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new ReviewUpdateDto { Rating = 4 };

        var result = await _sut.Update(10, dto, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Update_ReviewNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ReviewUpdateDto { Rating = 4 };
        _svcMock.Setup(s => s.UpdateAsync(10, 1, dto, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.Update(10, dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidRating_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new ReviewUpdateDto { Rating = 6 };
        _svcMock.Setup(s => s.UpdateAsync(10, 1, dto, default)).ThrowsAsync(new BusinessValidationException("Invalid rating"));

        var result = await _sut.Update(10, dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingReview_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.DeleteAsync(10, 1, default)).ReturnsAsync(true);

        var result = await _sut.Delete(10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_ReviewNotFound_Returns404()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.DeleteAsync(10, 1, default)).ReturnsAsync(false);

        var result = await _sut.Delete(10, default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.Delete(10, default);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
