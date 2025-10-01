using NUnit.Framework;
using Tests.Helpers;
using Data.Models;
using FluentAssertions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using DATA.Models;

namespace Tests.Models
{
    [TestFixture]
    public class AppUserTests : TestBase
    {
        [Test]
        public void AppUser_Creation_ShouldSetDefaultValues()
        {
            // Act
            var user = new AppUser();

            // Assert
            user.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            user.IsDeleted.Should().BeFalse();
            user.DateDeleted.Should().BeNull();
            user.Products.Should().NotBeNull().And.BeEmpty();
            user.Orders.Should().NotBeNull().And.BeEmpty();
            user.RefreshTokens.Should().NotBeNull().And.BeEmpty();
        }

        [Test]
        public void AppUser_WithRefreshTokens_ShouldManageCollectionCorrectly()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "testuser",
                Email = "test@example.com"
            };

            var refreshToken1 = new RefreshToken
            {
                Token = "token1",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow
            };

            var refreshToken2 = new RefreshToken
            {
                Token = "token2",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow
            };

            // Act
            user.RefreshTokens.Add(refreshToken1);
            user.RefreshTokens.Add(refreshToken2);

            // Assert
            user.RefreshTokens.Should().HaveCount(2);
            user.RefreshTokens.Should().Contain(refreshToken1);
            user.RefreshTokens.Should().Contain(refreshToken2);
        }

        [Test]
        public void AppUser_ActiveRefreshToken_ShouldReturnCorrectToken()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "testuser",
                Email = "test@example.com"
            };

            var activeToken = new RefreshToken
            {
                Token = "active-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            };

            var revokedToken = new RefreshToken
            {
                Token = "revoked-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow.AddMinutes(-5),
                RevokedOn = DateTime.UtcNow.AddMinutes(-1)
            };

            var expiredToken = new RefreshToken
            {
                Token = "expired-token",
                ExpiresOn = DateTime.UtcNow.AddDays(-1),
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            };

            user.RefreshTokens.Add(activeToken);
            user.RefreshTokens.Add(revokedToken);
            user.RefreshTokens.Add(expiredToken);

            // Act
            var activeRefreshToken = user.RefreshTokens.FirstOrDefault(r => r.IsActive);

            // Assert
            activeRefreshToken.Should().NotBeNull();
            activeRefreshToken.Should().Be(activeToken);
        }

        [Test]
        public void AppUser_SoftDelete_ShouldSetDeletedProperties()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "testuser",
                Email = "test@example.com"
            };

            var deletionTime = DateTime.UtcNow;

            // Act
            user.IsDeleted = true;
            user.DateDeleted = deletionTime;

            // Assert
            user.IsDeleted.Should().BeTrue();
            user.DateDeleted.Should().Be(deletionTime);
        }

        [Test]
        public void AppUser_Properties_ShouldBeSetCorrectly()
        {
            // Arrange
            var userName = "testuser";
            var email = "test@example.com";
            var createdDate = DateTime.UtcNow;

            // Act
            var user = new AppUser
            {
                UserName = userName,
                Email = email,
                CreatedDate = createdDate
            };

            // Assert
            user.UserName.Should().Be(userName);
            user.Email.Should().Be(email);
            user.CreatedDate.Should().Be(createdDate);
        }
    }
}