using Core.Helpers;
using CORE.DTOs.AppUser;
using CORE.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // Admin: list users with optional role filter
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAsync([FromQuery] UserFilterDto filter)
        {
            var result = await _userService.GetUsersAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // Admin: delete user
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // Authenticated: get own profile
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfileAsync()
        {
            var userId = UserHelpers.GetUserId(User);
            var result = await _userService.GetProfileAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        // Authenticated: update own profile
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfileAsync(UpdateUserProfileDto dto)
        {
            var userId = UserHelpers.GetUserId(User);
            var result = await _userService.UpdateProfileAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        // Authenticated: change password
        [HttpPut("me/password")]
        [Authorize]
        public async Task<IActionResult> UpdatePasswordAsync(UpdatePasswordDto dto)
        {
            var userId = UserHelpers.GetUserId(User);
            var result = await _userService.UpdatePasswordAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
