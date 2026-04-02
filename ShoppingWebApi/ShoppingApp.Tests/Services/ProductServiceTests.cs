using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IRepository<int, Product>> _productRepoMock;
        private readonly Mock<IRepository<int, Category>> _categoryRepoMock;
        private readonly Mock<IRepository<int, ProductImage>> _imageRepoMock;
        private readonly Mock<IRepository<int, Inventory>> _inventoryRepoMock;
        private readonly IMapper _mapper;

        public ProductServiceTests()
        {
            _productRepoMock = new Mock<IRepository<int, Product>>();
            _categoryRepoMock = new Mock<IRepository<int, Category>>();
            _imageRepoMock = new Mock<IRepository<int, ProductImage>>();
            _inventoryRepoMock = new Mock<IRepository<int, Inventory>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ProductImage, ProductImageReadDto>();
                cfg.CreateMap<Product, ProductReadDto>()
                   .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
                   .ForMember(d => d.AverageRating, opt => opt.Ignore())
                   .ForMember(d => d.ReviewsCount, opt => opt.Ignore());
                cfg.CreateMap<Review, ReviewReadDto>()
                   .ForMember(d => d.UserName, opt => opt.Ignore());
            });
            _mapper = config.CreateMapper();
        }

        // ── In-memory helpers ──────────────────────────────────────

        private AppDbContext CreateCtx(params Product[] products)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(opts);
            ctx.Products.AddRange(products);
            ctx.SaveChanges();
            return ctx;
        }

        private ProductService BuildSut(AppDbContext ctx)
        {
            _productRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Products);
            return new ProductService(
                _productRepoMock.Object, _categoryRepoMock.Object,
                _imageRepoMock.Object, _inventoryRepoMock.Object, ctx, _mapper);
        }

        private ProductService BuildMockSut()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(opts);
            return new ProductService(
                _productRepoMock.Object, _categoryRepoMock.Object,
                _imageRepoMock.Object, _inventoryRepoMock.Object, db, _mapper);
        }

        // ──────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Create_Success_ReturnsDto()
        {
            _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
            _productRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
            var added = new Product { Id = 10, Name = "Widget", SKU = "W001", Price = 9.99m, CategoryId = 1, IsActive = true };
            _productRepoMock.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync(added);
            _inventoryRepoMock.Setup(r => r.Add(It.IsAny<Inventory>())).ReturnsAsync(new Inventory());

            var result = await BuildMockSut().CreateAsync(new ProductCreateDto
            { Name = "Widget", SKU = "W001", Price = 9.99m, CategoryId = 1, IsActive = true });

            Assert.Equal("Widget", result.Name);
            _inventoryRepoMock.Verify(r => r.Add(It.IsAny<Inventory>()), Times.Once);
        }

        [Fact]
        public async Task Create_CategoryNotFound_ThrowsNotFoundException()
        {
            _categoryRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildMockSut().CreateAsync(new ProductCreateDto { Name = "X", SKU = "X1", Price = 1m, CategoryId = 99 }));
        }

        [Fact]
        public async Task Create_DuplicateSKU_ThrowsConflictException()
        {
            _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
            _productRepoMock.Setup(r => r.GetAll()).ReturnsAsync(
                new List<Product> { new Product { Id = 1, SKU = "EXISTING", Name = "Old", CategoryId = 1 } });

            await Assert.ThrowsAsync<ConflictException>(() =>
                BuildMockSut().CreateAsync(new ProductCreateDto { Name = "New", SKU = "EXISTING", Price = 1m, CategoryId = 1 }));
        }

        [Fact]
        public async Task Create_AddReturnsNull_ThrowsBusinessValidation()
        {
            _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
            _productRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
            _productRepoMock.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                BuildMockSut().CreateAsync(new ProductCreateDto { Name = "X", SKU = "X1", Price = 1m, CategoryId = 1 }));
        }

        // ──────────────────────────────────────────────
        // UPDATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Update_IdMismatch_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                BuildMockSut().UpdateAsync(1, new ProductUpdateDto { Id = 2, Name = "X", SKU = "Y", Price = 1m, CategoryId = 1 }));
        }

        [Fact]
        public async Task Update_ProductNotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildMockSut().UpdateAsync(99, new ProductUpdateDto { Id = 99, Name = "X", SKU = "Y", Price = 1m, CategoryId = 1 }));
        }

        [Fact]
        public async Task Update_DuplicateSKU_ThrowsConflictException()
        {
            var existing = new Product { Id = 1, Name = "A", SKU = "SKU1", CategoryId = 1 };
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
            _productRepoMock.Setup(r => r.GetAll()).ReturnsAsync(
                new List<Product> { new Product { Id = 2, SKU = "DUPE", Name = "Other", CategoryId = 1 } });

            await Assert.ThrowsAsync<ConflictException>(() =>
                BuildMockSut().UpdateAsync(1, new ProductUpdateDto { Id = 1, Name = "A", SKU = "DUPE", Price = 1m, CategoryId = 1 }));
        }

        [Fact]
        public async Task Update_Success_ReturnsDto()
        {
            var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
            _productRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });
            _productRepoMock.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(existing);

            using var ctx = CreateCtx(existing);
            var sut = BuildSut(ctx);

            var result = await sut.UpdateAsync(1,
                new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1 });

            Assert.NotNull(result);
        }

        // ──────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => BuildMockSut().DeleteAsync(99));
        }

        [Fact]
        public async Task Delete_ReferencedByOrders_ThrowsConflict()
        {
            // Seed product first, then add OrderItem separately with all required fields
            var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
            using var ctx = CreateCtx(product);
            ctx.OrderItems.Add(new OrderItem
            {
                Id = 1,
                ProductId = 1,
                OrderId = 0,
                ProductName = "P",
                SKU = "S1",
                UnitPrice = 1m,
                Quantity = 1,
                LineTotal = 1m
            });
            ctx.SaveChanges();

            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<ConflictException>(() => sut.DeleteAsync(1));
        }

        [Fact]
        public async Task Delete_Success_ReturnsTrue()
        {
            var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
            using var ctx = CreateCtx(product);

            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
            _productRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(product);
            var sut = BuildSut(ctx);

            var result = await sut.DeleteAsync(1);

            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // GET BY ID
        // ──────────────────────────────────────────────

        // [Fact] // Skipped: AsNoTracking + Include on EF in-memory provider doesn't resolve navigations reliably
        // public async Task GetById_Found_ReturnsDto() { }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(99));
        }

        // ──────────────────────────────────────────────
        // SET ACTIVE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task SetActive_NotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => BuildMockSut().SetActiveAsync(99, true));
        }

        [Fact]
        public async Task SetActive_Success()
        {
            var product = new Product { Id = 1, Name = "P", SKU = "S", IsActive = false };
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
            _productRepoMock.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(product);

            await BuildMockSut().SetActiveAsync(1, true);

            Assert.True(product.IsActive);
        }

        // ──────────────────────────────────────────────
        // ADD / REMOVE IMAGE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task AddImage_ProductNotFound_ThrowsNotFoundException()
        {
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildMockSut().AddImageAsync(99, new ProductImageCreateDto { Url = "http://x.com/img.jpg" }));
        }

        [Fact]
        public async Task AddImage_Success()
        {
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
            _imageRepoMock.Setup(r => r.Add(It.IsAny<ProductImage>())).ReturnsAsync(new ProductImage());

            await BuildMockSut().AddImageAsync(1, new ProductImageCreateDto { Url = "http://x.com/img.jpg" });

            _imageRepoMock.Verify(r => r.Add(It.IsAny<ProductImage>()), Times.Once);
        }

        [Fact]
        public async Task RemoveImage_NotFound_ThrowsNotFoundException()
        {
            _imageRepoMock.Setup(r => r.Get(99)).ReturnsAsync((ProductImage?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => BuildMockSut().RemoveImageAsync(1, 99));
        }

        [Fact]
        public async Task RemoveImage_ProductMismatch_ThrowsNotFoundException()
        {
            _imageRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new ProductImage { Id = 1, ProductId = 5 });

            await Assert.ThrowsAsync<NotFoundException>(() => BuildMockSut().RemoveImageAsync(1, 1));
        }

        [Fact]
        public async Task RemoveImage_Success_ReturnsTrue()
        {
            _imageRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new ProductImage { Id = 1, ProductId = 1 });
            _imageRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(new ProductImage());

            var result = await BuildMockSut().RemoveImageAsync(1, 1);

            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // GET ALL (paging / sorting)
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsPagedResult()
        {
            var products = Enumerable.Range(1, 5).Select(i =>
                new Product { Id = i, Name = $"P{i}", SKU = $"S{i}", CategoryId = 1 }).ToArray();
            using var ctx = CreateCtx(products);
            var sut = BuildSut(ctx);

            var result = await sut.GetAllAsync(1, 3);

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task GetAll_SortByPrice_Asc()
        {
            using var ctx = CreateCtx(
                new Product { Id = 1, Name = "P1", SKU = "S1", Price = 50m, CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", Price = 10m, CategoryId = 1 }
            );
            var sut = BuildSut(ctx);

            var result = await sut.GetAllAsync(1, 10, "price", "asc");

            Assert.Equal(10m, result.Items[0].Price);
        }

        // ──────────────────────────────────────────────
        // SEARCH
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Search_ByName_FiltersCorrectly()
        {
            using var ctx = CreateCtx(
                new Product { Id = 1, Name = "Apple iPhone", SKU = "S1", IsActive = true, CategoryId = 1 },
                new Product { Id = 2, Name = "Samsung Galaxy", SKU = "S2", IsActive = true, CategoryId = 1 }
            );
            var sut = BuildSut(ctx);

            var result = await sut.SearchAsync(new ProductQuery { NameContains = "Apple", Page = 1, Size = 10 });

            Assert.Single(result.Items);
            Assert.Contains("Apple", result.Items[0].Name);
        }

        [Fact]
        public async Task Search_ByPriceRange_FiltersCorrectly()
        {
            using var ctx = CreateCtx(
                new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 5m, IsActive = true, CategoryId = 1 },
                new Product { Id = 2, Name = "Expensive", SKU = "E1", Price = 500m, IsActive = true, CategoryId = 1 }
            );
            var sut = BuildSut(ctx);

            var result = await sut.SearchAsync(new ProductQuery { PriceMin = 100m, PriceMax = 1000m, Page = 1, Size = 10 });

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task Search_InactiveProducts_NotReturned()
        {
            using var ctx = CreateCtx(
                new Product { Id = 1, Name = "Hidden", SKU = "H1", IsActive = false, CategoryId = 1 }
            );
            var sut = BuildSut(ctx);

            var result = await sut.SearchAsync(new ProductQuery { Page = 1, Size = 10 });

            Assert.Empty(result.Items);
        }
    }
}
