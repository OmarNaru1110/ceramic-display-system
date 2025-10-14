using Core.DTOs.ReturnRequest;
using Core.Helpers;
using Core.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnRequestController : ControllerBase
    {
        private readonly IReturnRequestService _returnRequestService;

        public ReturnRequestController(IReturnRequestService returnRequestService)
        {
            _returnRequestService = returnRequestService;
        }

        [HttpPost]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateReturnRequestAsync(CreateReturnRequestDto dto)
        {
            var sellerId = UserHelpers.GetUserId(User);
            var result = await _returnRequestService.CreateReturnRequestAsync(dto, sellerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetReturnRequestByIdAsync(int id)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _returnRequestService.GetReturnRequestByIdAsync(id, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetReturnRequestsAsync([FromQuery] ReturnRequestFilterDto filter)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _returnRequestService.GetReturnRequestsAsync(filter, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/accept")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AcceptReturnRequestAsync(int id, [FromBody] string? adminNotes)
        {
            var adminId = UserHelpers.GetUserId(User);
            var result = await _returnRequestService.AcceptReturnRequestAsync(id, adminId, adminNotes);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReturnRequestAsync(int id, [FromBody] string? adminNotes)
        {
            var adminId = UserHelpers.GetUserId(User);
            var result = await _returnRequestService.RejectReturnRequestAsync(id, adminId, adminNotes);
            return StatusCode(result.StatusCode, result);
        }
    }
}
