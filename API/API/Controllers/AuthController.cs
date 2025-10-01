using CORE.Constants;
using CORE.DTOs.Auth;
using CORE.Services.IServices;
using Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterAsync(RegisterDto registerDto)
        {
            var roles = new List<string> { };
            if (registerDto.Role == UserRole.Seller)
                roles.Add(UserRole.Seller.ToString());
            else
                roles.Add(UserRole.Admin.ToString());
            var result = await _authService.RegisterAsync(registerDto, roles);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAccessTokenAsync(RefreshTokenDto dto)
        {
            var result = await _authService.RefreshAccessTokenAsync(dto.RefreshToken);
            return StatusCode(result.StatusCode, result);
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> RevokeRefreshTokenAsync(RefreshTokenDto dto)
        {
            var result = await _authService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return StatusCode(result.StatusCode, result);
        }
    }
}
