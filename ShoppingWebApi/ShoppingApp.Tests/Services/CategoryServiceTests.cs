using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly Mock<IRepository<int, Category>> _catRepoMock;
        private readonly Mock<IRepository<int, Product>> _prodRepoMock;
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly CategoryService _sut;

        private List<Category> _categories;
        private List<Product> _products;

        public CategoryServiceTests()
        {
            _catRepoMock = new Mock<IRepository<int, Category>>();
            _prodRepoMock = new Mock<IRepository<int, Product>>();

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(opts);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Category, CategoryReadDto>();
            });
            _mapper = config.CreateMapper();

            _categories = new List<Category>();
            _products = new List<Product>();

            _catRepoMock.Setup(r => r.GetQueryable()).Returns(() => _categories.AsQueryable());

            _sut = new CategoryService(_catRepoMock.Object, _prodRepoMock.Object, _db, _mapper);
        }

        public void Dispose() => _db.Dispose();

        // ──────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Create_NoParent_Success()
        {
            var entity = new Category { Id = 1, Name = "Electronics" };
            _catRepoMock.Setup(r => r.Add(It.IsAny<Category>())).ReturnsAsync(entity);

            var result = await _sut.CreateAsync(new CategoryCreateDto { Name = "Electronics" });

            Assert.Equal("Electronics", result.Name);
        }

        [Fact]
        public async Task Create_WithValidParent_Success()
        {
            var parent = new Category { Id = 5, Name = "Parent" };
            _catRepoMock.Setup(r => r.Get(5)).ReturnsAsync(parent);
            var child = new Category { Id = 6, Name = "Child", ParentCategoryId = 5 };
            _catRepoMock.Setup(r => r.Add(It.IsAny<Category>())).ReturnsAsync(child);

            var result = await _sut.CreateAsync(new CategoryCreateDto { Name = "Child", ParentCategoryId = 5 });

            Assert.Equal("Child", result.Name);
        }

        [Fact]
        public async Task Create_WithInvalidParent_ThrowsNotFoundException()
        {
            _catRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CategoryCreateDto { Name = "X", ParentCategoryId = 99 }));
        }

        [Fact]
        public async Task Create_AddReturnsNull_ThrowsBusinessValidation()
        {
            _catRepoMock.Setup(r => r.Add(It.IsAny<Category>())).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.CreateAsync(new CategoryCreateDto { Name = "X" }));
        }

        // ──────────────────────────────────────────────
        // UPDATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Update_IdMismatch_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 2, Name = "X" }));
        }

        [Fact]
        public async Task Update_NotFound_ThrowsNotFoundException()
        {
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "X" }));
        }

        [Fact]
        public async Task Update_SelfParent_ThrowsBusinessValidation()
        {
            var cat = new Category { Id = 1, Name = "Cat" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "Cat", ParentCategoryId = 1 }));
        }

        [Fact]
        public async Task Update_ParentNotFound_ThrowsNotFoundException()
        {
            var cat = new Category { Id = 1, Name = "Cat" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);
            _catRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "Cat", ParentCategoryId = 99 }));
        }

        [Fact]
        public async Task Update_CyclicParent_ThrowsBusinessValidation()
        {
            // Cat1 -> Cat2 -> Cat1 would be a cycle
            var cat1 = new Category { Id = 1, Name = "Cat1" };
            var cat2 = new Category { Id = 2, Name = "Cat2", ParentCategoryId = 1 };

            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat1);
            _catRepoMock.Setup(r => r.Get(2)).ReturnsAsync(cat2);

            // Trying to set cat1's parent to cat2 creates cycle (cat2 already has cat1 as parent)
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "Cat1", ParentCategoryId = 2 }));
        }

        [Fact]
        public async Task Update_Success()
        {
            var cat = new Category { Id = 1, Name = "Old" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);
            _catRepoMock.Setup(r => r.Update(1, It.IsAny<Category>()))
                        .ReturnsAsync(new Category { Id = 1, Name = "New" });

            var result = await _sut.UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "New" });

            Assert.NotNull(result);
            Assert.Equal("New", result!.Name);
        }

        // ──────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            _catRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }

        [Fact]
        public async Task Delete_HasChildren_ThrowsConflict()
        {
            var cat = new Category { Id = 1, Name = "Parent" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);
            _categories.Add(new Category { Id = 2, Name = "Child", ParentCategoryId = 1 });
            _catRepoMock.Setup(r => r.GetAll())
                        .ReturnsAsync(_categories);

            await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task Delete_HasProducts_ThrowsConflict()
        {
            var cat = new Category { Id = 1, Name = "Cat" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);
            _catRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Category>());
            _products.Add(new Product { Id = 10, CategoryId = 1, Name = "P1", SKU = "S1" });
            _prodRepoMock.Setup(r => r.GetAll()).ReturnsAsync(_products);

            await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task Delete_Success_ReturnsTrue()
        {
            var cat = new Category { Id = 1, Name = "Cat" };
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(cat);
            _catRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Category>());
            _prodRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
            _catRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(cat);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // GET BY ID
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetById_Found_ReturnsDto()
        {
            _catRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Electronics" });

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Electronics", result!.Name);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            _catRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ──────────────────────────────────────────────
        // GET ALL (uses real in-memory DB)
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetAll_DefaultPaging_ReturnsPagedResult()
        {
            _db.Categories.AddRange(
                new Category { Name = "A" },
                new Category { Name = "B" },
                new Category { Name = "C" }
            );
            await _db.SaveChangesAsync();

            var result = await _sut.GetAllAsync(1, 2);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetAll_InvalidPage_DefaultsToOne()
        {
            _db.Categories.Add(new Category { Name = "A" });
            await _db.SaveChangesAsync();

            var result = await _sut.GetAllAsync(-5, -5);

            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }
    }
}
