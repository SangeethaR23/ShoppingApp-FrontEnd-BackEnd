using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Wishlist;

namespace ShoppingWebApi.Interfaces
{
    public interface IWishlistService
    {
        Task<bool> ToggleAsync(int userId, int productId, CancellationToken ct = default);
        Task<IEnumerable<WishlistReadDto>> GetAsync(int userId, CancellationToken ct = default);
        Task<bool> MoveToCartAsync(int userId, int productId, CancellationToken ct = default);
    }
}