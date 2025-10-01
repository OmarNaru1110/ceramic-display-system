using NUnit.Framework;
using Tests.Helpers;
using DATA.DataAccess.Context;
using Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using DATA.Models;
using Microsoft.AspNetCore.Identity;

namespace Tests.Integration
{
    [TestFixture]
    public class DatabaseIntegrationTests : TestBase
    {
        [Test]
        public async Task AppDbContext_CanCreateAndRetrieveUser()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "integrationtestuser",
                Email = "integration@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            var retrievedUser = await Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "integrationtestuser");

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.UserName.Should().Be("integrationtestuser");
            retrievedUser.Email.Should().Be("integration@example.com");
            retrievedUser.EmailConfirmed.Should().BeTrue();
        }

        [Test]
        public async Task AppDbContext_CanManageRefreshTokens()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "refreshtokenuser",
                Email = "refreshtoken@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var refreshToken = new RefreshToken
            {
                Token = "test-token-hash",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow
            };

            user.RefreshTokens.Add(refreshToken);

            // Act
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            var retrievedUser = await Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "refreshtokenuser");

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.RefreshTokens.Should().HaveCount(1);
            retrievedUser.RefreshTokens.Should().Contain(rt => rt.Token == "test-token-hash");
        }

        [Test]
        public async Task AppDbContext_SoftDeleteUser_ShouldSetIsDeleted()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "softdeleteuser",
                Email = "softdelete@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            user.IsDeleted = true;
            user.DateDeleted = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            var retrievedUser = await Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "softdeleteuser");

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.IsDeleted.Should().BeTrue();
            retrievedUser.DateDeleted.Should().NotBeNull();
            retrievedUser.DateDeleted.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task AppDbContext_UpdateRefreshToken_ShouldPersistChanges()
        {
            // Arrange
            var user = new AppUser
            {
                UserName = "updatetokenuser",
                Email = "updatetoken@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var refreshToken = new RefreshToken
            {
                Token = "original-token",
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                CreatedOn = DateTime.UtcNow
            };

            user.RefreshTokens.Add(refreshToken);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var retrievedUser = await Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "updatetokenuser");

            var tokenToUpdate = retrievedUser.RefreshTokens.First();
            tokenToUpdate.RevokedOn = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            // Re-retrieve to verify persistence
            var finalUser = await Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "updatetokenuser");

            // Assert
            finalUser.RefreshTokens.Should().HaveCount(1);
            finalUser.RefreshTokens.First().RevokedOn.Should().NotBeNull();
            finalUser.RefreshTokens.First().IsActive.Should().BeFalse();
        }

        [Test]
        public async Task AppDbContext_MultipleUsers_ShouldBeIndependent()
        {
            // Arrange
            var user1 = new AppUser
            {
                UserName = "user1",
                Email = "user1@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var user2 = new AppUser
            {
                UserName = "user2",
                Email = "user2@example.com",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
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

            user1.RefreshTokens.Add(refreshToken1);
            user2.RefreshTokens.Add(refreshToken2);

            // Act
            Context.Users.AddRange(user1, user2);
            await Context.SaveChangesAsync();

            var retrievedUsers = await Context.Users.ToListAsync();

            // Assert
            retrievedUsers.Should().HaveCount(2);
            retrievedUsers[0].RefreshTokens.Should().HaveCount(1);
            retrievedUsers[1].RefreshTokens.Should().HaveCount(1);
            retrievedUsers[0].RefreshTokens.First().Token.Should().NotBe(retrievedUsers[1].RefreshTokens.First().Token);
        }
    }
}