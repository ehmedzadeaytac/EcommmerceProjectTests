using ECommerceAfternoon.Server.DTOs.Cart;
using ECommerceAfternoon.Server.Models;

namespace ECommerceAfternoon.Server.Services.Abstract
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<ServiceResult> AddToCartAsync(int userId, AddToCartDto dto);
        Task<ServiceResult> UpdateQuantityAsync(int userId, int productId, int quantity);
        Task<ServiceResult> RemoveFromCartAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
    }
}