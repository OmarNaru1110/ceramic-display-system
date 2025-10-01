using AutoMapper;
using CORE.DTOs.Auth;
using CORE.Services;
using Data.Models;
using DATA.DataAccess.Context;
using DATA.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Data.DataAccess.Repositories.UnitOfWork;

namespace Tests.Helpers
{
    public class TestBase
    {
        protected AppDbContext Context { get; private set; } = null!;
        protected UserManager<AppUser> UserManager { get; private set; } = null!;
        protected IConfiguration Configuration { get; private set; } = null!;
        protected Mock<IConfiguration> MockConfiguration { get; private set; } = null!;
        protected Mock<IOptions<JwtConfig>> MockJwtOptions { get; private set; } = null!;
        protected Mock<IMapper> MockMapper { get; private set; } = null!;
        protected Mock<IUnitOfWork> MockUnitOfWork { get; private set; } = null!;
        protected IMemoryCache MemoryCache { get; private set; } = null!;
        protected Mock<ILogger<AuthService>> MockLogger { get; private set; } = null!;

        [SetUp]
        public virtual async Task SetUp()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new AppDbContext(options);

            // Ensure database is created
            await Context.Database.EnsureCreatedAsync();

            // Setup UserManager
            var userStore = new Mock<IUserStore<AppUser>>();
            userStore.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityResult.Success);

            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<AppUser>>();
            var userValidators = new List<IUserValidator<AppUser>>();
            var passwordValidators = new List<IPasswordValidator<AppUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<AppUser>>>();

            var mockUserManager = new Mock<UserManager<AppUser>>(
                userStore.Object,
                optionsAccessor.Object,
                passwordHasher.Object,
                userValidators,
                passwordValidators,
                keyNormalizer.Object,
                errors.Object,
                services.Object,
                logger.Object);

            UserManager = mockUserManager.Object;

            // Setup Configuration using ConfigurationBuilder for real IConfiguration
            var configData = new Dictionary<string, string?>
            {
                ["RefreshTokenDurationInDays"] = "7"
            };

            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Keep MockConfiguration for backwards compatibility if needed
            MockConfiguration = new Mock<IConfiguration>();
            MockConfiguration.Setup(x => x["RefreshTokenDurationInDays"]).Returns("7");

            // Setup JWT Options - Use the correct JwtConfig from Core
            var jwtConfig = new JwtConfig
            {
                Key = "this-is-a-very-secure-key-that-is-at-least-32-characters-long-for-testing-purposes",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                DurationInHours = 1
            };

            MockJwtOptions = new Mock<IOptions<JwtConfig>>();
            MockJwtOptions.Setup(x => x.Value).Returns(jwtConfig);

            // Setup Mapper
            MockMapper = new Mock<IMapper>();

            // Setup UnitOfWork
            MockUnitOfWork = new Mock<IUnitOfWork>();

            // Setup MemoryCache
            MemoryCache = new MemoryCache(new MemoryCacheOptions());

            // Setup Logger
            MockLogger = new Mock<ILogger<AuthService>>();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Context?.Dispose();
            MemoryCache?.Dispose();
        }

        protected AppUser CreateTestUser(string username = "testuser", string email = "test@example.com")
        {
            return new AppUser
            {
                Id = 1,
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow,
                RefreshTokens = new List<RefreshToken>()
            };
        }

        protected RegisterDto CreateRegisterDto(string username = "testuser", string email = "test@example.com")
        {
            return new RegisterDto
            {
                UserName = username,
                Email = email,
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = Data.Models.Enums.UserRole.Seller
            };
        }

        protected LoginDto CreateLoginDto(string usernameOrEmail = "testuser", string password = "Test123!")
        {
            return new LoginDto
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password
            };
        }
    }
}