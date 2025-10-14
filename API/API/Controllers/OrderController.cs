using Core.DTOs.Order;
using Core.Helpers;
using Core.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateOrderAsync(CreateOrderDto dto)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _orderService.CreateOrderAsync(dto, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{orderId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateOrderAsync(int orderId, UpdateOrderDto dto)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _orderService.UpdateOrderAsync(orderId, dto, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{orderId}/cancel")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CancelOrderAsync(int orderId)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _orderService.CancelOrderAsync(orderId, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderByIdAsync(int orderId)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _orderService.GetOrderByIdAsync(orderId, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrdersAsync([FromQuery] OrderFilterDto filter)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _orderService.GetOrdersAsync(filter, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{orderId}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmOrderAsync(int orderId)
        {
            var adminId = UserHelpers.GetUserId(User);
            var result = await _orderService.ConfirmOrderAsync(orderId, adminId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{orderId}/complete")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CompleteOrderAsync(int orderId)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _orderService.CompleteOrderAsync(orderId, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{orderId}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectOrderAsync(int orderId, [FromBody] string? reason)
        {
            var adminId = UserHelpers.GetUserId(User);
            var result = await _orderService.RejectOrderAsync(orderId, adminId, reason);
            return StatusCode(result.StatusCode, result);
        }
    }
}
