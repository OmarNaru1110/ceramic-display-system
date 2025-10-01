using NUnit.Framework;
using Tests.Helpers;
using DATA.Models;
using FluentAssertions;
using System;

namespace Tests.Models
{
    [TestFixture]
    public class RefreshTokenTests : TestBase
    {
        [Test]
        public void RefreshToken_IsActive_WhenNotExpiredAndNotRevoked_ShouldReturnTrue()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            };

            // Act & Assert
            refreshToken.IsActive.Should().BeTrue();
        }

        [Test]
        public void RefreshToken_IsActive_WhenExpired_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                ExpiresOn = DateTime.UtcNow.AddDays(-1), // Expired
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            };

            // Act & Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        [Test]
        public void RefreshToken_IsActive_WhenRevoked_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = DateTime.UtcNow // Revoked
            };

            // Act & Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        [Test]
        public void RefreshToken_IsExpired_WhenExpirationDatePassed_ShouldReturnTrue()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(-1), // Expired 1 minute ago
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };

            // Act & Assert
            refreshToken.IsExpired.Should().BeTrue();
        }

        [Test]
        public void RefreshToken_IsExpired_WhenNotExpired_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7), // Expires in 7 days
                CreatedOn = DateTime.UtcNow
            };

            // Act & Assert
            refreshToken.IsExpired.Should().BeFalse();
        }

        [Test]
        public void RefreshToken_Creation_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var token = "hashed-token-value";
            var rawToken = "raw-token-value";
            var expiresOn = DateTime.UtcNow.AddDays(7);
            var createdOn = DateTime.UtcNow;

            // Act
            var refreshToken = new RefreshToken
            {
                Token = token,
                RawToken = rawToken,
                ExpiresOn = expiresOn,
                CreatedOn = createdOn
            };

            // Assert
            refreshToken.Token.Should().Be(token);
            refreshToken.RawToken.Should().Be(rawToken);
            refreshToken.ExpiresOn.Should().Be(expiresOn);
            refreshToken.CreatedOn.Should().Be(createdOn);
            refreshToken.RevokedOn.Should().BeNull();
        }
    }
}