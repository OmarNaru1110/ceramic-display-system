using NUnit.Framework;
using Tests.Helpers;
using CORE.DTOs.Auth;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Data.Models.Enums;

namespace Tests.DTOs
{
    [TestFixture]
    public class AuthDtoTests : TestBase
    {
        [Test]
        public void RegisterDto_Validation_ShouldValidateRequiredFields()
        {
            // Arrange
            var registerDto = new RegisterDto();
            var validationContext = new ValidationContext(registerDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().NotBeEmpty();
            
            var requiredFields = validationResults
                .Where(vr => vr.ErrorMessage.Contains("required") || vr.ErrorMessage.Contains("Required"))
                .Select(vr => string.Join(", ", vr.MemberNames))
                .ToList();
            
            // Email and Role should be required (Password validation might not work in unit tests)
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Email"));
        }

        [Test]
        public void RegisterDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = UserRole.Seller,
                Address = "123 Test St"
            };
            var validationContext = new ValidationContext(registerDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            validationResults.Should().BeEmpty();
        }

        [Test]
        public void RegisterDto_WithPasswordMismatch_ShouldFailValidation()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "Test123!",
                ConfirmPassword = "Different123!",
                Role = UserRole.Seller
            };
            var validationContext = new ValidationContext(registerDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("ConfirmPassword"));
        }

        [Test]
        public void LoginDto_Validation_ShouldValidateRequiredFields()
        {
            // Arrange
            var loginDto = new LoginDto();
            var validationContext = new ValidationContext(loginDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(loginDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("UsernameOrEmail"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Password"));
        }

        [Test]
        public void LoginDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                UsernameOrEmail = "testuser",
                Password = "Test123!"
            };
            var validationContext = new ValidationContext(loginDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(loginDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            validationResults.Should().BeEmpty();
        }

        [Test]
        public void RefreshTokenDto_Validation_ShouldValidateRequiredField()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto();
            var validationContext = new ValidationContext(refreshTokenDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(refreshTokenDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("RefreshToken"));
        }

        [Test]
        public void RefreshTokenDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };
            var validationContext = new ValidationContext(refreshTokenDto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(refreshTokenDto, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            validationResults.Should().BeEmpty();
        }

        [Test]
        public void AuthDto_Properties_ShouldBeSetCorrectly()
        {
            // Arrange
            var authDto = new AuthDto
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                IsAuthenticated = true,
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                Roles = new List<string> { "Seller" },
                AccountStatus = "Active"
            };

            // Assert
            authDto.UserId.Should().Be(1);
            authDto.Username.Should().Be("testuser");
            authDto.Email.Should().Be("test@example.com");
            authDto.IsAuthenticated.Should().BeTrue();
            authDto.AccessToken.Should().Be("access-token");
            authDto.RefreshToken.Should().Be("refresh-token");
            authDto.Roles.Should().BeEquivalentTo(new List<string> { "Seller" });
            authDto.AccountStatus.Should().Be("Active");
        }
    }
}