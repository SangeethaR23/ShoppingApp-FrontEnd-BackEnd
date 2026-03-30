using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class ReviewServiceTests
    {
        private readonly Mock<IRepository<int, Review>> _reviewRepoMock;
        private readonly Mock<IRepository<int, Product>> _productRepoMock;
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ReviewService>> _loggerMock;

        public ReviewServiceTests()
        {
            _reviewRepoMock = new Mock<IRepository<int, Review>>();
            _productRepoMock = new Mock<IRepository<int, Product>>();
            _userRepoMock = new Mock<IRepository<int, User>>();
            _loggerMock = new Mock<ILogger<ReviewService>>();

            var config = new MapperConfiguration(cfg =>
                cfg.CreateMap<Review, ReviewReadDto>().ForMember(d => d.UserName, opt => opt.Ignore()));
            _mapper = config.CreateMapper();
        }

        // Creates an isolated in-memory DbContext seeded with the given reviews
        private AppDbContext CreateCtx(params Review[] reviews)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(opts);
            ctx.Reviews.AddRange(reviews);
            ctx.SaveChanges();
            return ctx;
        }

        private ReviewService BuildSut(AppDbContext ctx)
        {
            _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Reviews);
            return new ReviewService(
                _reviewRepoMock.Object, _productRepoMock.Object,
                _userRepoMock.Object, _mapper, _loggerMock.Object);
        }

        // ──────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Create_ProductNotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                sut.CreateAsync(new ReviewCreateDto { ProductId = 99, UserId = 1, Rating = 5 }));
        }

        [Fact]
        public async Task Create_UserNotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
            _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                sut.CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 99, Rating = 4 }));
        }

        [Fact]
        public async Task Create_DuplicateReview_ThrowsConflict()
        {
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            using var ctx = CreateCtx(new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 4 });
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<ConflictException>(() =>
                sut.CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 1, Rating = 5 }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public async Task Create_InvalidRating_ThrowsBusinessValidation(int rating)
        {
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            using var ctx = CreateCtx(); // no existing review so duplicate check passes
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                sut.CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 1, Rating = rating }));
        }

        [Fact]
        public async Task Create_ValidReview_Success()
        {
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            var newReview = new Review { Id = 5, ProductId = 1, UserId = 1, Rating = 4, Comment = "Great!" };
            _reviewRepoMock.Setup(r => r.Add(It.IsAny<Review>())).ReturnsAsync(newReview);
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            var result = await sut.CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 1, Rating = 4, Comment = "Great!" });

            Assert.Equal(4, result.Rating);
        }

        // ──────────────────────────────────────────────
        // GET (single)
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Get_Found_ReturnsDto()
        {
            using var ctx = CreateCtx(new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 5 });
            var sut = BuildSut(ctx);

            var result = await sut.GetAsync(1, 1);

            Assert.NotNull(result);
            Assert.Equal(5, result!.Rating);
        }

        [Fact]
        public async Task Get_NotFound_ReturnsNull()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            var result = await sut.GetAsync(1, 99);

            Assert.Null(result);
        }

        // ──────────────────────────────────────────────
        // GET BY PRODUCT (paged)
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetByProduct_ReturnsPagedResult()
        {
            using var ctx = CreateCtx(
                new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3 },
                new Review { Id = 2, ProductId = 1, UserId = 2, Rating = 5 },
                new Review { Id = 3, ProductId = 2, UserId = 3, Rating = 4 }
            );
            var sut = BuildSut(ctx);

            var result = await sut.GetByProductAsync(1, 1, 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetByProduct_Paging_Works()
        {
            var reviews = Enumerable.Range(1, 5)
                .Select(i => new Review { Id = i, ProductId = 1, UserId = i, Rating = 3 })
                .ToArray();
            using var ctx = CreateCtx(reviews);
            var sut = BuildSut(ctx);

            var result = await sut.GetByProductAsync(1, 2, 2);

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
        }

        // ──────────────────────────────────────────────
        // UPDATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Update_ReviewNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                sut.UpdateAsync(1, 99, new ReviewUpdateDto { Rating = 4 }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task Update_InvalidRating_ThrowsBusinessValidation(int rating)
        {
            using var ctx = CreateCtx(new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3 });
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                sut.UpdateAsync(1, 1, new ReviewUpdateDto { Rating = rating }));
        }

        [Fact]
        public async Task Update_Success_ReturnsTrue()
        {
            var review = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3 };
            using var ctx = CreateCtx(review);
            var sut = BuildSut(ctx);
            _reviewRepoMock.Setup(r => r.Update(1, It.IsAny<Review>())).ReturnsAsync(review);

            var result = await sut.UpdateAsync(1, 1, new ReviewUpdateDto { Rating = 5, Comment = "Updated" });

            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Delete_NotFound_ReturnsFalse()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            var result = await sut.DeleteAsync(1, 99);

            Assert.False(result);
        }

        [Fact]
        public async Task Delete_Success_ReturnsTrue()
        {
            var review = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 4 };
            using var ctx = CreateCtx(review);
            var sut = BuildSut(ctx);
            _reviewRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(review);

            var result = await sut.DeleteAsync(1, 1);

            Assert.True(result);
        }
    }
}
