using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Wishlist;

namespace ShoppingWebApi.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<int, WishlistItem> _wishlistRepo;
        private readonly IRepository<int, Product> _productRepo;
        private readonly IRepository<int, Carts> _cartRepo;
        private readonly IRepository<int, CartItem> _cartItemRepo;

        public WishlistService(
            IRepository<int, WishlistItem> wishlistRepo,
            IRepository<int, Product> productRepo,
            IRepository<int, Carts> cartRepo,
            IRepository<int, CartItem> cartItemRepo)
        {
            _wishlistRepo = wishlistRepo;
            _productRepo = productRepo;
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
        }

        // TOGGLE (Add / Remove)
        public async Task<bool> ToggleAsync(int userId, int productId, CancellationToken ct = default)
        {
            var existing = await _wishlistRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

            if (existing != null)
            {
                await _wishlistRepo.Delete(existing.Id);
                return false; // removed
            }

            var item = new WishlistItem
            {
                UserId = userId,
                ProductId = productId,
                CreatedUtc = DateTime.UtcNow
            };

            await _wishlistRepo.Add(item);
            return true; // added
        }

        // GET WISHLIST
        public async Task<IEnumerable<WishlistReadDto>> GetAsync(int userId, CancellationToken ct = default)
        {
            var items = await _wishlistRepo.GetQueryable()
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .ToListAsync(ct);

            return items.Select(w => new WishlistReadDto
            {
                ProductId = w.ProductId,
                ProductName = w.Product.Name,
                SKU = w.Product.SKU,
                Price = w.Product.Price,
                IsActive = w.Product.IsActive
            });
        }

        // MOVE TO CART
        public async Task<bool> MoveToCartAsync(int userId, int productId, CancellationToken ct = default)
        {
            var cart = await _cartRepo.GetQueryable()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart == null)
            {
                cart = new Carts
                {
                    UserId = userId
                };
                cart = await _cartRepo.Add(cart);
            }

            // check existing cart item
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += 1;
                await _cartItemRepo.Update(existing.Id, existing);
            }
            else
            {
                var product = await _productRepo.Get(productId);
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = product?.Price ?? 0m,
                    CreatedUtc = DateTime.UtcNow
                };
                await _cartItemRepo.Add(newItem);
            }

            // remove from wishlist
            var wishItem = await _wishlistRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

            if (wishItem != null)
                await _wishlistRepo.Delete(wishItem.Id);

            return true;
        }
    }
}