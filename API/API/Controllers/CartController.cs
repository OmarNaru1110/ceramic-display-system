using Core.DTOs.Cart;
using Core.Helpers;
using Core.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartAsync()
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _cartService.GetCartAsync(sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItemAsync(AddCartItemDto dto)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _cartService.AddItemToCartAsync(dto, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItemAsync(int cartItemId, UpdateCartItemDto dto)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _cartService.UpdateCartItemAsync(cartItemId, dto, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItemAsync(int cartItemId)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _cartService.RemoveCartItemAsync(cartItemId, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCartAsync()
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _cartService.ClearCartAsync(sellerId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
