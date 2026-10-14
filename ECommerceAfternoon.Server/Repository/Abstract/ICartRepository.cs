using ECommerceAfternoon.Server.Entities;

namespace ECommerceAfternoon.Server.Repository.Abstract
{
    public interface ICartRepository
    {
        Task<Cart?> GetCartByUserIdAsync(int userId);
        Task<CartItem?> GetCartItemAsync(int userId, int productId);
        Task<Product?> GetProductByIdAsync(int productId);
        Task AddCartAsync(Cart cart);
        void RemoveCartItem(CartItem cartItem);
        void RemoveCartItems(IEnumerable<CartItem> cartItems);
        Task SaveChangesAsync();
    }
}