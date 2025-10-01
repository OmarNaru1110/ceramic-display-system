using NUnit.Framework;
using Tests.Helpers;
using API.Controllers;
using CORE.Services.IServices;
using Moq;
using Microsoft.AspNetCore.Mvc;
using CORE.DTOs.Auth;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using CORE.DTOs;
using CORE.Constants;
using Data.Models.Enums;

namespace Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests : TestBase
    {
        private AuthController _authController = null!;
        private Mock<IAuthService> _mockAuthService = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            
            _mockAuthService = new Mock<IAuthService>();
            _authController = new AuthController(_mockAuthService.Object);
        }

        #region RegisterAsync Tests

        [Test]
        public async Task RegisterAsync_WithSellerRole_ShouldCallAuthServiceWithSellerRole()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = UserRole.Seller
            };

            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.Created,
                Message = "Registered Successfully.",
                Data = new AuthDto
                {
                    UserId = 1,
                    Username = registerDto.UserName,
                    Email = registerDto.Email,
                    IsAuthenticated = true,
                    Roles = new List<string> { "Seller" }
                }
            };

            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.Is<List<string>>(r => r.Contains("Seller"))))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RegisterAsync(registerDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Created);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Data!.Roles.Should().Contain("Seller");

            _mockAuthService.Verify(s => s.RegisterAsync(registerDto, It.Is<List<string>>(r => r.Contains("Seller"))), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WithAdminRole_ShouldCallAuthServiceWithAdminRole()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                UserName = "adminuser",
                Email = "admin@example.com",
                Password = "Admin123!",
                ConfirmPassword = "Admin123!",
                Role = UserRole.Admin
            };

            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.Created,
                Message = "Registered Successfully.",
                Data = new AuthDto
                {
                    UserId = 1,
                    Username = registerDto.UserName,
                    Email = registerDto.Email,
                    IsAuthenticated = true,
                    Roles = new List<string> { "Admin" }
                }
            };

            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.Is<List<string>>(r => r.Contains("Admin"))))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RegisterAsync(registerDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Created);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Data!.Roles.Should().Contain("Admin");

            _mockAuthService.Verify(s => s.RegisterAsync(registerDto, It.Is<List<string>>(r => r.Contains("Admin"))), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WithFailure_ShouldReturnErrorResponse()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "Email already exists.",
                Data = null
            };

            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<List<string>>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RegisterAsync(registerDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.BadRequest);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("Email already exists.");
            responseData.Data.Should().BeNull();
        }

        #endregion

        #region LoginAsync Tests

        [Test]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessResponse()
        {
            // Arrange
            var loginDto = CreateLoginDto();
            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.OK,
                Message = "Logged in successfully.",
                Data = new AuthDto
                {
                    UserId = 1,
                    Username = "testuser",
                    Email = "test@example.com",
                    IsAuthenticated = true,
                    AccessToken = "sample-jwt-token",
                    RefreshToken = "sample-refresh-token",
                    Roles = new List<string> { "Seller" }
                }
            };

            _mockAuthService.Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.LoginAsync(loginDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.OK);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("Logged in successfully.");
            responseData.Data.Should().NotBeNull();
            responseData.Data!.AccessToken.Should().NotBeNullOrEmpty();
            responseData.Data.RefreshToken.Should().NotBeNullOrEmpty();

            _mockAuthService.Verify(s => s.LoginAsync(loginDto), Times.Once);
        }

        [Test]
        public async Task LoginAsync_WithInvalidCredentials_ShouldReturnBadRequest()
        {
            // Arrange
            var loginDto = CreateLoginDto();
            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "Username or password is incorrect.",
                Data = null
            };

            _mockAuthService.Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.LoginAsync(loginDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.BadRequest);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("Username or password is incorrect.");
            responseData.Data.Should().BeNull();
        }

        #endregion

        #region RefreshAccessTokenAsync Tests

        [Test]
        public async Task RefreshAccessTokenAsync_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };
            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.OK,
                Data = new AuthDto
                {
                    UserId = 1,
                    Username = "testuser",
                    Email = "test@example.com",
                    IsAuthenticated = true,
                    AccessToken = "new-jwt-token",
                    RefreshToken = "new-refresh-token",
                    Roles = new List<string> { "Seller" }
                }
            };

            _mockAuthService.Setup(s => s.RefreshAccessTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RefreshAccessTokenAsync(refreshTokenDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.OK);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Data.Should().NotBeNull();
            responseData.Data!.AccessToken.Should().NotBeNullOrEmpty();
            responseData.Data.RefreshToken.Should().NotBeNullOrEmpty();

            _mockAuthService.Verify(s => s.RefreshAccessTokenAsync(refreshTokenDto.RefreshToken), Times.Once);
        }

        [Test]
        public async Task RefreshAccessTokenAsync_WithInvalidToken_ShouldReturnBadRequest()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto { RefreshToken = "invalid-refresh-token" };
            var expectedResponse = new ResponseDto<AuthDto>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "Invalid token.",
                Data = null
            };

            _mockAuthService.Setup(s => s.RefreshAccessTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RefreshAccessTokenAsync(refreshTokenDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.BadRequest);
            
            var responseData = objectResult.Value as ResponseDto<AuthDto>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("Invalid token.");
            responseData.Data.Should().BeNull();
        }

        #endregion

        #region RevokeRefreshTokenAsync Tests

        [Test]
        public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldReturnSuccess()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };
            var expectedResponse = new ResponseDto<object>
            {
                StatusCode = StatusCodes.OK,
                Message = "You have been logged out. Please delete your access token and refresh token."
            };

            _mockAuthService.Setup(s => s.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RevokeRefreshTokenAsync(refreshTokenDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.OK);
            
            var responseData = objectResult.Value as ResponseDto<object>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("You have been logged out. Please delete your access token and refresh token.");

            _mockAuthService.Verify(s => s.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken), Times.Once);
        }

        [Test]
        public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldReturnBadRequest()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto { RefreshToken = "invalid-refresh-token" };
            var expectedResponse = new ResponseDto<object>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "Invalid token."
            };

            _mockAuthService.Setup(s => s.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authController.RevokeRefreshTokenAsync(refreshTokenDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.BadRequest);
            
            var responseData = objectResult.Value as ResponseDto<object>;
            responseData.Should().NotBeNull();
            responseData!.Message.Should().Be("Invalid token.");
        }

        #endregion
    }
}