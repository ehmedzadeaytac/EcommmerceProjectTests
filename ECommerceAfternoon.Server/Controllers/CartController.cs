using ECommerceAfternoon.Server.DTOs.Cart;
using ECommerceAfternoon.Server.Models;
using ECommerceAfternoon.Server.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAfternoon.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("{userId:int}/items")]
        public async Task<IActionResult> AddToCart(int userId, AddToCartDto dto)
        {
            var result = await _cartService.AddToCartAsync(userId, dto);
            return ToActionResult(result, Ok());
        }

        [HttpPut("{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> UpdateQuantity(
            int userId,
            int productId,
            [FromQuery] int quantity)
        {
            var result = await _cartService.UpdateQuantityAsync(userId, productId, quantity);
            return ToActionResult(result, Ok());
        }

        [HttpDelete("{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(int userId, int productId)
        {
            var result = await _cartService.RemoveFromCartAsync(userId, productId);
            return ToActionResult(result, NoContent());
        }

        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            await _cartService.ClearCartAsync(userId);
            return NoContent();
        }

        private IActionResult ToActionResult(ServiceResult result, IActionResult onSuccess)
        {
            return result.Status switch
            {
                ServiceResultStatus.Success => onSuccess,
                ServiceResultStatus.NotFound => NotFound(result.Message),
                _ => BadRequest(result.Message)
            };
        }
    }
}