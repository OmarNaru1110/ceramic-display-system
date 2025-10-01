using NUnit.Framework;
using Tests.Helpers;
using CORE.Services;
using Moq;
using Microsoft.AspNetCore.Identity;
using Data.Models;
using CORE.DTOs.Auth;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using CORE.Constants;
using System;
using System.Linq;
using CORE.DTOs;
using DATA.Models;
using Microsoft.EntityFrameworkCore;
using DATA.DataAccess.Repositories.IRepositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Tests.Services
{
    [TestFixture]
    public class AuthServiceTests : TestBase
    {
        private AuthService _authService = null!;
        private Mock<UserManager<AppUser>> _mockUserManager = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            
            // Create a proper mock for UserManager with all required parameters
            var userStore = new Mock<IUserStore<AppUser>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<AppUser>>();
            var userValidators = new List<IUserValidator<AppUser>>();
            var passwordValidators = new List<IPasswordValidator<AppUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<AppUser>>>();

            _mockUserManager = new Mock<UserManager<AppUser>>(
                userStore.Object,
                optionsAccessor.Object,
                passwordHasher.Object,
                userValidators,
                passwordValidators,
                keyNormalizer.Object,
                errors.Object,
                services.Object,
                logger.Object);

            _authService = new AuthService(
                MockJwtOptions.Object,
                _mockUserManager.Object,
                Configuration, // Use the real IConfiguration instead of MockConfiguration
                MockMapper.Object,
                MockUnitOfWork.Object,
                MemoryCache,
                MockLogger.Object);
        }

        #region RegisterAsync Tests

        [Test]
        public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var roles = new List<string> { "Seller" };
            var user = CreateTestUser();

            MockMapper.Setup(m => m.Map<AppUser>(registerDto)).Returns(user);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string>());
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<AppUser>(), roles))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _authService.RegisterAsync(registerDto, roles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.Created);
            result.Message.Should().Be("Registered Successfully.");
            result.Data.Should().NotBeNull();
            result.Data!.IsAuthenticated.Should().BeTrue();
            result.Data.Username.Should().Be(registerDto.UserName);
            result.Data.Email.Should().Be(registerDto.Email);
            result.Data.Roles.Should().BeEquivalentTo(roles);
        }

        [Test]
        public async Task RegisterAsync_WithExistingUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var existingUser = CreateTestUser();
            var roles = new List<string> { "Seller" };

            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName)).ReturnsAsync(existingUser);

            // Act
            var result = await _authService.RegisterAsync(registerDto, roles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Username already exists.");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task RegisterAsync_WithExistingEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var existingUser = CreateTestUser();
            var roles = new List<string> { "Seller" };

            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            // Act
            var result = await _authService.RegisterAsync(registerDto, roles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Email already exists.");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task RegisterAsync_WithUserCreationFailure_ShouldReturnInternalServerError()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var roles = new List<string> { "Seller" };
            var user = CreateTestUser();
            var identityError = new IdentityError { Description = "Password too weak" };

            MockMapper.Setup(m => m.Map<AppUser>(registerDto)).Returns(user);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.RegisterAsync(registerDto, roles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Contain("Password too weak");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task RegisterAsync_WithRoleAssignmentFailure_ShouldDeleteUserAndReturnInternalServerError()
        {
            // Arrange
            var registerDto = CreateRegisterDto();
            var roles = new List<string> { "Seller" };
            var user = CreateTestUser();
            var roleError = new IdentityError { Description = "Role does not exist" };

            MockMapper.Setup(m => m.Map<AppUser>(registerDto)).Returns(user);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string>());
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<AppUser>(), roles))
                .ReturnsAsync(IdentityResult.Failed(roleError));
            _mockUserManager.Setup(m => m.DeleteAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(registerDto, roles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Contain("Role does not exist");
            result.Data.Should().BeNull();

            _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<AppUser>()), Times.Once);
        }

        #endregion

        #region LoginAsync Tests

        [Test]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessWithTokens()
        {
            // Arrange
            var loginDto = CreateLoginDto();
            var user = CreateTestUser();
            var roles = new List<string> { "Seller" };

            _mockUserManager.Setup(m => m.FindByNameAsync(loginDto.UsernameOrEmail)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Message.Should().Be("Logged in successfully.");
            result.Data.Should().NotBeNull();
            result.Data!.IsAuthenticated.Should().BeTrue();
            result.Data.Username.Should().Be(user.UserName);
            result.Data.Email.Should().Be(user.Email);
            result.Data.Roles.Should().BeEquivalentTo(roles);
            result.Data.AccessToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        }

        [Test]
        public async Task LoginAsync_WithEmailInsteadOfUsername_ShouldReturnSuccess()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                UsernameOrEmail = "test@example.com",
                Password = "Test123!"
            };
            var user = CreateTestUser();
            var roles = new List<string> { "Seller" };

            _mockUserManager.Setup(m => m.FindByNameAsync(loginDto.UsernameOrEmail)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.UsernameOrEmail)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.IsAuthenticated.Should().BeTrue();
        }

        [Test]
        public async Task LoginAsync_WithInvalidUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var loginDto = CreateLoginDto("nonexistent", "Test123!");

            _mockUserManager.Setup(m => m.FindByNameAsync(loginDto.UsernameOrEmail)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.UsernameOrEmail)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Username or password is incorrect.");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var loginDto = CreateLoginDto("testuser", "WrongPassword!");
            var user = CreateTestUser();

            _mockUserManager.Setup(m => m.FindByNameAsync(loginDto.UsernameOrEmail)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Username or password is incorrect.");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task LoginAsync_WithUserWithoutRoles_ShouldReturnInternalServerError()
        {
            // Arrange
            var loginDto = CreateLoginDto();
            var user = CreateTestUser();

            _mockUserManager.Setup(m => m.FindByNameAsync(loginDto.UsernameOrEmail)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Be("User has no roles, Try logging in again.");
            result.Data.Should().BeNull();
        }

        #endregion

        #region UpdateUserRolesAsync Tests

        [Test]
        public async Task UpdateUserRolesAsync_WithValidUser_ShouldReturnSuccess()
        {
            // Arrange
            var userId = 1;
            var newRoles = new List<string> { "Admin" };
            var user = CreateTestUser();
            var mockRepository = new Mock<IBaseRepository<AppUser>>();

            mockRepository.Setup(r => r.GetAsync(userId)).ReturnsAsync(user);
            MockUnitOfWork.Setup(u => u.AppUsers).Returns(mockRepository.Object);

            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Seller" });
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(user, newRoles))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.UpdateUserRolesAsync(userId, newRoles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Message.Should().Be("Roles updated successfully.");
        }

        [Test]
        public async Task UpdateUserRolesAsync_WithInvalidUser_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = 999;
            var newRoles = new List<string> { "Admin" };
            var mockRepository = new Mock<IBaseRepository<AppUser>>();

            mockRepository.Setup(r => r.GetAsync(userId)).ReturnsAsync((AppUser?)null);
            MockUnitOfWork.Setup(u => u.AppUsers).Returns(mockRepository.Object);

            // Act
            var result = await _authService.UpdateUserRolesAsync(userId, newRoles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("User not found.");
        }

        [Test]
        public async Task UpdateUserRolesAsync_WithRoleUpdateFailure_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var newRoles = new List<string> { "Admin" };
            var user = CreateTestUser();
            var mockRepository = new Mock<IBaseRepository<AppUser>>();
            var identityError = new IdentityError { Description = "Role update failed" };

            mockRepository.Setup(r => r.GetAsync(userId)).ReturnsAsync(user);
            MockUnitOfWork.Setup(u => u.AppUsers).Returns(mockRepository.Object);

            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Seller" });
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(user, newRoles))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.UpdateUserRolesAsync(userId, newRoles);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Contain("Role update failed");
        }

        #endregion

        #region Helper Methods

        private static Mock<DbSet<AppUser>> CreateMockDbSet(IQueryable<AppUser> data)
        {
            var mockDbSet = new Mock<DbSet<AppUser>>();
            mockDbSet.As<IQueryable<AppUser>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<AppUser>(data.Provider));
            mockDbSet.As<IQueryable<AppUser>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<AppUser>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<AppUser>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockDbSet.As<IAsyncEnumerable<AppUser>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<AppUser>(data.GetEnumerator()));
            return mockDbSet;
        }

        #endregion
    }

    // Helper classes for async testing with DbSet
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var result = Execute<TResult>(expression);
            return new ValueTask<TResult>(result);
        }

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Execute<TResult>(expression);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return default;
        }
    }
}
