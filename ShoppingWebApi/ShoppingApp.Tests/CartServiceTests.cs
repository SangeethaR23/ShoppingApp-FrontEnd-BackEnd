using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Cart;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class CartServiceTests : IDisposable
    {
        private readonly Mock<IRepository<int, Carts>> _cartRepoMock;
        private readonly Mock<IRepository<int, CartItem>> _cartItemRepoMock;
        private readonly Mock<IRepository<int, Product>> _productRepoMock;
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly CartService _sut;

        public CartServiceTests()
        {
            _cartRepoMock = new Mock<IRepository<int, Carts>>();
            _cartItemRepoMock = new Mock<IRepository<int, CartItem>>();
            _productRepoMock = new Mock<IRepository<int, Product>>();
            _userRepoMock = new Mock<IRepository<int, User>>();

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(opts);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CartItem, CartItemReadDto>()
                   .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : ""))
                   .ForMember(d => d.SKU, opt => opt.MapFrom(s => s.Product != null ? s.Product.SKU : ""))
                   .ForMember(d => d.LineTotal, opt => opt.MapFrom(s => s.UnitPrice * s.Quantity))
                   .ForMember(d => d.AverageRating, opt => opt.Ignore())
                   .ForMember(d => d.ReviewsCount, opt => opt.Ignore());
                cfg.CreateMap<Carts, CartReadDto>()
                   .ForMember(d => d.SubTotal, opt => opt.MapFrom(s => s.Items.Sum(i => i.UnitPrice * i.Quantity)));
            });
            _mapper = config.CreateMapper();

            _sut = new CartService(
                _cartRepoMock.Object, _cartItemRepoMock.Object,
                _productRepoMock.Object, _userRepoMock.Object,
                _db, _mapper);
        }

        public void Dispose() => _db.Dispose();

        // ──────────────────────────────────────────────
        // GET BY USER ID
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetByUserId_NoCart_ReturnsEmptyDto()
        {
            // No cart exists in DB
            var result = await _sut.GetByUserIdAsync(999);

            Assert.Equal(0, result.Id);
            Assert.Equal(999, result.UserId);
            Assert.Empty(result.Items);
            Assert.Equal(0m, result.SubTotal);
        }

        [Fact]
        public async Task GetByUserId_WithCart_ReturnsCartDto()
        {
            // Seed in-memory DB
            var product = new Product { Id = 1, Name = "Widget", SKU = "W1", Price = 10m, CategoryId = 1 };
            var cart = new Carts { Id = 1, UserId = 5 };
            var item = new CartItem { Id = 1, CartId = 1, ProductId = 1, Quantity = 2, UnitPrice = 10m, Product = product };
            cart.Items.Add(item);

            _db.Products.Add(product);
            _db.Carts.Add(cart);
            _db.CartItems.Add(item);
            await _db.SaveChangesAsync();

            var result = await _sut.GetByUserIdAsync(5);

            Assert.Equal(1, result.Id);
            Assert.Single(result.Items);
            Assert.Equal(20m, result.SubTotal);
        }

        // ──────────────────────────────────────────────
        // ADD ITEM
        // ──────────────────────────────────────────────

        [Fact]
        public async Task AddItem_UserNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.AddItemAsync(99, new CartAddItemDto { ProductId = 1, Quantity = 1 }));
        }

        [Fact]
        public async Task AddItem_ProductNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.AddItemAsync(1, new CartAddItemDto { ProductId = 99, Quantity = 1 }));
        }

        [Fact]
        public async Task AddItem_InactiveProduct_ThrowsBusinessValidation()
        {
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, IsActive = false, Name = "P", SKU = "S" });

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 1 }));
        }

        [Fact]
        public async Task AddItem_NewCart_CreatesCartAndAddsItem()
        {
            var user = new User { Id = 1, Email = "a@b.com" };
            var product = new Product { Id = 1, Name = "P", SKU = "S", Price = 5m, IsActive = true, CategoryId = 1 };
            var newCart = new Carts { Id = 10, UserId = 1 };

            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(user);
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());
            _cartRepoMock.Setup(r => r.Add(It.IsAny<Carts>())).ReturnsAsync(newCart);
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());
            _cartItemRepoMock.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

            // For the final GetByUserIdAsync call (returns empty)
            var result = await _sut.AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 2 });

            _cartRepoMock.Verify(r => r.Add(It.IsAny<Carts>()), Times.Once);
            _cartItemRepoMock.Verify(r => r.Add(It.IsAny<CartItem>()), Times.Once);
        }

        [Fact]
        public async Task AddItem_ExistingItem_IncreasesQuantity()
        {
            var user = new User { Id = 1, Email = "a@b.com" };
            var product = new Product { Id = 1, Name = "P", SKU = "S", Price = 5m, IsActive = true, CategoryId = 1 };
            var cart = new Carts { Id = 10, UserId = 1 };
            var existingItem = new CartItem { Id = 1, CartId = 10, ProductId = 1, Quantity = 1, UnitPrice = 5m };

            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(user);
            _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem> { existingItem });
            _cartItemRepoMock.Setup(r => r.Update(1, It.IsAny<CartItem>())).ReturnsAsync(existingItem);

            await _sut.AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 3 });

            // Quantity should now be 4
            Assert.Equal(4, existingItem.Quantity);
        }

        // ──────────────────────────────────────────────
        // UPDATE ITEM
        // ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateItem_CartNotFound_ThrowsNotFoundException()
        {
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 2 }));
        }

        [Fact]
        public async Task UpdateItem_ItemNotFound_ThrowsNotFoundException()
        {
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 99, Quantity = 2 }));
        }

        [Fact]
        public async Task UpdateItem_QuantityZero_DeletesItem()
        {
            var cart = new Carts { Id = 1, UserId = 1 };
            var item = new CartItem { Id = 5, CartId = 1, ProductId = 1, Quantity = 2 };

            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem> { item });
            _cartItemRepoMock.Setup(r => r.Delete(5)).ReturnsAsync(item);

            await _sut.UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 0 });

            _cartItemRepoMock.Verify(r => r.Delete(5), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_NonZeroQuantity_UpdatesItem()
        {
            var cart = new Carts { Id = 1, UserId = 1 };
            var item = new CartItem { Id = 5, CartId = 1, ProductId = 1, Quantity = 2 };

            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem> { item });
            _cartItemRepoMock.Setup(r => r.Update(5, It.IsAny<CartItem>())).ReturnsAsync(item);

            await _sut.UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 5 });

            Assert.Equal(5, item.Quantity);
        }

        // ──────────────────────────────────────────────
        // REMOVE ITEM
        // ──────────────────────────────────────────────

        [Fact]
        public async Task RemoveItem_CartNotFound_ThrowsNotFoundException()
        {
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveItemAsync(1, 1));
        }

        [Fact]
        public async Task RemoveItem_ItemNotFound_ThrowsNotFoundException()
        {
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveItemAsync(1, 99));
        }

        [Fact]
        public async Task RemoveItem_Success_DeletesItem()
        {
            var cart = new Carts { Id = 1, UserId = 1 };
            var item = new CartItem { Id = 5, CartId = 1, ProductId = 1 };

            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem> { item });
            _cartItemRepoMock.Setup(r => r.Delete(5)).ReturnsAsync(item);

            await _sut.RemoveItemAsync(1, 1);

            _cartItemRepoMock.Verify(r => r.Delete(5), Times.Once);
        }

        // ──────────────────────────────────────────────
        // CLEAR CART
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Clear_NoCart_DoesNotThrow()
        {
            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());

            // Should not throw
            await _sut.ClearAsync(999);
        }

        [Fact]
        public async Task Clear_WithItems_DeletesAllItems()
        {
            var cart = new Carts { Id = 1, UserId = 1 };
            var items = new List<CartItem>
            {
                new CartItem { Id = 1, CartId = 1, ProductId = 1 },
                new CartItem { Id = 2, CartId = 1, ProductId = 2 }
            };

            _cartRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
            _cartItemRepoMock.Setup(r => r.GetAll()).ReturnsAsync(items);
            _cartItemRepoMock.Setup(r => r.Delete(It.IsAny<int>())).ReturnsAsync(new CartItem());

            await _sut.ClearAsync(1);

            _cartItemRepoMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Exactly(2));
        }
    }
}
