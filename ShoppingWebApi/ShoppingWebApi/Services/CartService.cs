using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Common;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Cart;

namespace ShoppingWebApi.Services
{
    public class CartService : ICartService
    {
        private readonly IRepository<int, Carts> _cartRepo;
        private readonly IRepository<int, CartItem> _cartItemRepo;
        private readonly IRepository<int, Product> _productRepo;
        private readonly IRepository<int, User> _userRepo;

        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public CartService(
            IRepository<int, Carts> cartRepo,
            IRepository<int, CartItem> cartItemRepo,
            IRepository<int, Product> productRepo,
            IRepository<int, User> userRepo,
            AppDbContext db,
            IMapper mapper)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _productRepo = productRepo;
            _userRepo = userRepo;
            _db = db;
            _mapper = mapper;
        }

        // --------------------------------------------------------------------
        // READ CART
        // --------------------------------------------------------------------
        public async Task<CartReadDto> GetByUserIdAsync(int userId, CancellationToken ct = default)
        {
            var cart = await _db.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart == null)
            {
                return new CartReadDto
                {
                    Id = 0,
                    UserId = userId,
                    Items = new(),
                    SubTotal = 0m
                };
            }

            var dto = _mapper.Map<CartReadDto>(cart);

            if (dto.Items.Count > 0)
            {
                var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

                // rating summary
                var ratingLookup = await _db.Reviews
                    .Where(r => productIds.Contains(r.ProductId))
                    .GroupBy(r => r.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Avg = g.Average(r => (double)r.Rating),
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.ProductId, ct);

                foreach (var item in dto.Items)
                {
                    if (ratingLookup.TryGetValue(item.ProductId, out var r))
                    {
                        item.AverageRating = Math.Round(r.Avg, 2);
                        item.ReviewsCount = r.Count;
                    }
                }

                dto.SubTotal = dto.Items.Sum(i => i.LineTotal);
            }

            return dto;
        }

        // --------------------------------------------------------------------
        // ADD ITEM
        // --------------------------------------------------------------------
        public async Task<CartReadDto> AddItemAsync(int userId, CartAddItemDto dto, CancellationToken ct = default)
        {
            var user = await _userRepo.Get(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            var product = await _productRepo.Get(dto.ProductId);
            if (product == null)
                throw new NotFoundException("Product not found.");

            if (!product.IsActive)
                throw new BusinessValidationException("Product is inactive.");

            await using var tx = await _db.Database.BeginTransactionSafeAsync(ct);
            try
            {
                // Find or create cart
                var carts = await _cartRepo.GetAll() ?? Enumerable.Empty<Carts>();
                var cart = carts.FirstOrDefault(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = await _cartRepo.Add(new Carts { UserId = userId });
                    if (cart == null)
                        throw new BusinessValidationException("Failed to create cart.");
                }

                // Get all items of this cart
                var items = await _cartItemRepo.GetAll() ?? Enumerable.Empty<CartItem>();
                var existingItem = items.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == dto.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += dto.Quantity;
                    existingItem.UpdatedUtc = DateTime.UtcNow;
                    await _cartItemRepo.Update(existingItem.Id, existingItem);
                }
                else
                {
                    await _cartItemRepo.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = product.Price
                    });
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            return await GetByUserIdAsync(userId, ct);
        }

        // --------------------------------------------------------------------
        // UPDATE ITEM (BUG FIXED)
        // --------------------------------------------------------------------
        public async Task<CartReadDto> UpdateItemAsync(int userId, CartUpdateItemDto dto, CancellationToken ct = default)
        {
            var carts = await _cartRepo.GetAll() ?? Enumerable.Empty<Carts>();
            var cart = carts.FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
                throw new NotFoundException("Cart not found.");

            var items = await _cartItemRepo.GetAll() ?? Enumerable.Empty<CartItem>();
            var item = items.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == dto.ProductId);

            if (item == null)
                throw new NotFoundException("Cart item not found.");

            if (dto.Quantity == 0)
            {
                await _cartItemRepo.Delete(item.Id);
            }
            else
            {
                item.Quantity = dto.Quantity;
                item.UpdatedUtc = DateTime.UtcNow;
                await _cartItemRepo.Update(item.Id, item);
            }

            return await GetByUserIdAsync(userId, ct);
        }

        // --------------------------------------------------------------------
        // REMOVE ITEM (SAFE)
        // --------------------------------------------------------------------
        public async Task RemoveItemAsync(int userId, int productId, CancellationToken ct = default)
        {
            var carts = await _cartRepo.GetAll() ?? Enumerable.Empty<Carts>();
            var cart = carts.FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
                throw new NotFoundException("Cart not found.");

            var items = await _cartItemRepo.GetAll() ?? Enumerable.Empty<CartItem>();
            var item = items.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == productId);

            if (item == null)
                throw new NotFoundException("Cart item not found.");

            await _cartItemRepo.Delete(item.Id);
        }

        // --------------------------------------------------------------------
        // CLEAR CART (BUG FIXED)
        // --------------------------------------------------------------------
        public async Task ClearAsync(int userId, CancellationToken ct = default)
        {
            var carts = await _cartRepo.GetAll() ?? Enumerable.Empty<Carts>();
            var cart = carts.FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
                return;

            var items = await _cartItemRepo.GetAll() ?? Enumerable.Empty<CartItem>();
            var userItems = items.Where(i => i.CartId == cart.Id).ToList();

            await using var tx = await _db.Database.BeginTransactionSafeAsync(ct);
            try
            {
                foreach (var item in userItems)
                    await _cartItemRepo.Delete(item.Id);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
