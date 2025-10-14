using CORE.Services;
using CORE.Services.IServices;
using DATA.DataAccess.Context;
using DATA.Models;
using Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Helpers;

namespace Tests.Services
{
    [TestFixture]
    public class RefreshTokenCleanupServiceTests : TestBase
    {
        private Mock<ILogger<RefreshTokenCleanupService>> _mockLogger;
        private IRefreshTokenCleanupService _cleanupService;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _mockLogger = new Mock<ILogger<RefreshTokenCleanupService>>();
            _cleanupService = new RefreshTokenCleanupService(_mockLogger.Object, Context);
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithNoTokens_ShouldReturnZero()
        {
            // Arrange - No users or tokens in database

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(0);
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithOnlyActiveTokens_ShouldReturnZero()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "activeuser",
                Email = "active@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var activeToken = new RefreshToken
            {
                Token = "active-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            };

            user.RefreshTokens.Add(activeToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(0);

            // Verify token still exists
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "activeuser");
            userAfter.RefreshTokens.Should().HaveCount(1);
            userAfter.RefreshTokens.First().Token.Should().Be("active-token-hash");
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithExpiredTokens_ShouldDeleteThem()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "expireduser",
                Email = "expired@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var expiredToken = new RefreshToken
            {
                Token = "expired-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            };

            user.RefreshTokens.Add(expiredToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(1);

            // Verify token was deleted
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "expireduser");
            userAfter.RefreshTokens.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithRevokedTokens_ShouldDeleteThem()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "revokeduser",
                Email = "revoked@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var revokedToken = new RefreshToken
            {
                Token = "revoked-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(7), // Not expired
                CreatedOn = DateTime.UtcNow,
                RevokedOn = DateTime.UtcNow.AddMinutes(-5) // But revoked
            };

            user.RefreshTokens.Add(revokedToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(1);

            // Verify token was deleted
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "revokeduser");
            userAfter.RefreshTokens.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithMixedTokens_ShouldDeleteOnlyInactive()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "mixeduser",
                Email = "mixed@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var activeToken = new RefreshToken
            {
                Token = "active-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            };

            var expiredToken = new RefreshToken
            {
                Token = "expired-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(-1),
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            };

            var revokedToken = new RefreshToken
            {
                Token = "revoked-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(5),
                CreatedOn = DateTime.UtcNow.AddMinutes(-10),
                RevokedOn = DateTime.UtcNow.AddMinutes(-5)
            };

            user.RefreshTokens.Add(activeToken);
            user.RefreshTokens.Add(expiredToken);
            user.RefreshTokens.Add(revokedToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(2);

            // Verify only active token remains
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "mixeduser");
            userAfter.RefreshTokens.Should().HaveCount(1);
            userAfter.RefreshTokens.First().Token.Should().Be("active-token-hash");
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithMultipleUsers_ShouldDeleteFromAll()
        {
            // Arrange
            var user1 = new AppUser
            {
                UserName = "user1",
                Email = "user1@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var user2 = new AppUser
            {
                UserName = "user2",
                Email = "user2@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            // User1: 1 active, 2 expired
            user1.RefreshTokens.Add(new RefreshToken
            {
                Token = "user1-active",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            });

            user1.RefreshTokens.Add(new RefreshToken
            {
                Token = "user1-expired1",
                ExpiresOn = DateTime.UtcNow.AddDays(-1),
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            });

            user1.RefreshTokens.Add(new RefreshToken
            {
                Token = "user1-expired2",
                ExpiresOn = DateTime.UtcNow.AddDays(-2),
                CreatedOn = DateTime.UtcNow.AddDays(-9),
                RevokedOn = null
            });

            // User2: 1 revoked
            user2.RefreshTokens.Add(new RefreshToken
            {
                Token = "user2-revoked",
                ExpiresOn = DateTime.UtcNow.AddDays(5),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = DateTime.UtcNow.AddMinutes(-1)
            });

            Context.Users.AddRange(user1, user2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(3);

            // Verify user1 has only active token
            var user1After = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "user1");
            user1After.RefreshTokens.Should().HaveCount(1);
            user1After.RefreshTokens.First().Token.Should().Be("user1-active");

            // Verify user2 has no tokens
            var user2After = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "user2");
            user2After.RefreshTokens.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithExpiredAndRevokedToken_ShouldDeleteIt()
        {
            // Arrange - Token that is both expired and revoked
            var user = new AppUser
            {
                UserName = "doubleuser",
                Email = "double@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var expiredAndRevokedToken = new RefreshToken
            {
                Token = "expired-and-revoked-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(-1),
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = DateTime.UtcNow.AddDays(-1)
            };

            user.RefreshTokens.Add(expiredAndRevokedToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(1);

            // Verify token was deleted
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "doubleuser");
            userAfter.RefreshTokens.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WhenRunMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "idempotentuser",
                Email = "idempotent@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var expiredToken = new RefreshToken
            {
                Token = "expired-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(-1),
                CreatedOn = DateTime.UtcNow.AddDays(-8),
                RevokedOn = null
            };

            user.RefreshTokens.Add(expiredToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act - Run cleanup multiple times
            var result1 = await _cleanupService.DeleteInactiveRefreshTokensAsync();
            var result2 = await _cleanupService.DeleteInactiveRefreshTokensAsync();
            var result3 = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result1.Should().Be(1);
            result2.Should().Be(0);
            result3.Should().Be(0);
        }

        [Test]
        public async Task DeleteInactiveRefreshTokensAsync_WithLargeNumberOfTokens_ShouldHandleEfficiently()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "bulkuser",
                Email = "bulk@test.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            // Add 50 expired tokens and 1 active token
            for (int i = 0; i < 50; i++)
            {
                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = $"expired-token-{i}",
                    ExpiresOn = DateTime.UtcNow.AddDays(-1),
                    CreatedOn = DateTime.UtcNow.AddDays(-8),
                    RevokedOn = null
                });
            }

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = "active-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow,
                RevokedOn = null
            });

            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cleanupService.DeleteInactiveRefreshTokensAsync();

            // Assert
            result.Should().Be(50);

            // Verify only active token remains
            var userAfter = await Context.Users.FirstOrDefaultAsync(u => u.UserName == "bulkuser");
            userAfter.RefreshTokens.Should().HaveCount(1);
            userAfter.RefreshTokens.First().Token.Should().Be("active-token");
        }
    }
}
