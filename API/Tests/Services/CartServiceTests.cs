using NUnit.Framework;
using Tests.Helpers;
using Core.Services;
using Core.DTOs.Cart;
using CORE.DTOs;
using Data.Models;
using DATA.DataAccess.Repositories.IRepositories;
using Data.DataAccess.Repositories.UnitOfWork;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StatusCodes = CORE.Constants.StatusCodes;

namespace Tests.Services
{
    [TestFixture]
    public class CartServiceTests : TestBase
    {
        private CartService _service = null!;
        private Mock<IBaseRepository<Cart>> _mockCartRepo = null!;
        private Mock<IBaseRepository<CartItem>> _mockCartItemRepo = null!;
        private Mock<IBaseRepository<Product>> _mockProductRepo = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();

            _mockCartRepo = new Mock<IBaseRepository<Cart>>();
            _mockCartItemRepo = new Mock<IBaseRepository<CartItem>>();
            _mockProductRepo = new Mock<IBaseRepository<Product>>();

            MockUnitOfWork.Setup(u => u.Carts).Returns(_mockCartRepo.Object);
            MockUnitOfWork.Setup(u => u.CartItems).Returns(_mockCartItemRepo.Object);
            MockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);

            _service = new CartService(MockUnitOfWork.Object, MockMapper.Object);
        }

        #region GetCartAsync
        [Test]
        public async Task GetCartAsync_Unauthorized_WhenSellerIdNull()
        {
            var result = await _service.GetCartAsync(null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
            result.Message.Should().Be("Invalid seller ID");
        }

        [Test]
        public async Task GetCartAsync_Unauthorized_WhenSellerIdInvalid()
        {
            var result = await _service.GetCartAsync(0);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
            result.Message.Should().Be("Invalid seller ID");
        }

        [Test]
        public async Task GetCartAsync_CreatesCart_WhenNotExists()
        {
            _mockCartRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Cart?)null);

            _mockCartRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync((Cart c) => { c.Id = 123; return c; });

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.GetCartAsync(7);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.SellerId.Should().Be(7);
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalAmount.Should().Be(0);
        }

        [Test]
        public async Task GetCartAsync_ReturnsExistingCart_WithTotals()
        {
            var cart = new Cart
            {
                Id = 1,
                SellerId = 5,
                CreatedDate = DateTime.UtcNow,
                CartItems = new List<CartItem>
                {
                    new CartItem{ Id = 1, CartId = 1, ProductId = 10, Quantity = 2, Product = new Product{ Id = 10, Name = "A", PricePerSqm = 12.5m }},
                    new CartItem{ Id = 2, CartId = 1, ProductId = 11, Quantity = 3, Product = new Product{ Id = 11, Name = "B", PricePerSqm = 20m }}
                }
            };

            _mockCartRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(cart);

            var result = await _service.GetCartAsync(5);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalAmount.Should().Be(2 * 12.5m + 3 * 20m);
        }
        #endregion

        #region AddItemToCartAsync
        [Test]
        public async Task AddItemToCartAsync_Unauthorized_WhenSellerIdNull()
        {
            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 1 }, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task AddItemToCartAsync_Unauthorized_WhenSellerIdInvalid()
        {
            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 1 }, 0);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task AddItemToCartAsync_BadRequest_WhenQuantityNotPositive()
        {
            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 0 }, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Quantity must be greater than 0");
        }

        [Test]
        public async Task AddItemToCartAsync_NotFound_WhenProductMissing()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Product?)null);
            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 1 }, 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Product not found");
        }

        [Test]
        public async Task AddItemToCartAsync_NotFound_WhenProductDeleted()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(new Product{ Id = 1, IsDeleted = true });
            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 1 }, 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Product not found");
        }

        [Test]
        public async Task AddItemToCartAsync_BadRequest_WhenNewQuantityExceedsStock()
        {
            var product = new Product{ Id = 1, Quantity = 6 };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(product);

            // Cart exists with an existing item of quantity 2
            var existingCart = new Cart
            {
                Id = 10,
                SellerId = 9,
                CartItems = new List<CartItem>{ new CartItem{ Id = 100, CartId = 10, ProductId = 1, Quantity = 2 } }
            };

            _mockCartRepo
                .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(existingCart); // first for Add

            var dto = new AddCartItemDto{ ProductId = 1, Quantity = 5 }; // new total 7 > 6
            var result = await _service.AddItemToCartAsync(dto, 9);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Contain("Insufficient stock");
        }

        [Test]
        public async Task AddItemToCartAsync_CreatesCart_WhenCartMissing()
        {
            var product = new Product{ Id = 1, Name = "P", Quantity = 10, PricePerSqm = 5m };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(product);

            var createdCart = new Cart { Id = 20, SellerId = 3, CartItems = new List<CartItem>{ new CartItem{ Id = 200, CartId = 20, ProductId = 1, Quantity = 2, Product = product } } };

            _mockCartRepo
                .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Cart?)null)   // first call in Add to check for existing cart
                .ReturnsAsync(createdCart);  // second call from GetCartAsync

            _mockCartRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync((Cart c) => { c.Id = 20; return c; });

            _mockCartItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<CartItem>()))
                .ReturnsAsync((CartItem ci) => { ci.Id = 200; return ci; });

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 1, Quantity = 2 }, 3);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data!.Items.Should().HaveCount(1);
            result.Data.TotalAmount.Should().Be(2 * 5m);
        }

        [Test]
        public async Task AddItemToCartAsync_AddsNewItem_WhenNoExistingItem()
        {
            var product = new Product{ Id = 2, Name = "Q", Quantity = 5, PricePerSqm = 3m };
            _mockProductRepo.Setup(r => r.GetAsync(2)).ReturnsAsync(product);

            var cart = new Cart { Id = 30, SellerId = 4, CartItems = new List<CartItem>() };

            _mockCartRepo
                .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(cart)
                .ReturnsAsync(new Cart{ Id = 30, SellerId = 4, CartItems = new List<CartItem>{ new CartItem{ Id = 300, CartId = 30, ProductId = 2, Quantity = 1, Product = product } } });

            _mockCartItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<CartItem>()))
                .ReturnsAsync((CartItem ci) => { ci.Id = 300; return ci; });

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 2, Quantity = 1 }, 4);

            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockCartItemRepo.Verify(r => r.AddOrUpdateAsync(It.Is<CartItem>(ci => ci.ProductId == 2 && ci.Quantity == 1 && ci.CartId == 30)), Times.Once);
            result.Data!.TotalAmount.Should().Be(1 * 3m);
        }

        [Test]
        public async Task AddItemToCartAsync_UpdatesExistingItem_WhenPresent_AndWithinStock()
        {
            var product = new Product{ Id = 3, Name = "R", Quantity = 10, PricePerSqm = 2m };
            _mockProductRepo.Setup(r => r.GetAsync(3)).ReturnsAsync(product);

            var existing = new CartItem{ Id = 400, CartId = 40, ProductId = 3, Quantity = 4 };
            var cart = new Cart { Id = 40, SellerId = 6, CartItems = new List<CartItem>{ existing } };

            _mockCartRepo
                .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(cart)
                .ReturnsAsync(new Cart{ Id = 40, SellerId = 6, CartItems = new List<CartItem>{ new CartItem{ Id = 400, CartId = 40, ProductId = 3, Quantity = 6, Product = product } } });

            _mockCartItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<CartItem>()))
                .ReturnsAsync((CartItem ci) => ci);

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 3, Quantity = 2 }, 6);

            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockCartItemRepo.Verify(r => r.AddOrUpdateAsync(It.Is<CartItem>(ci => ci.Id == 400 && ci.Quantity == 6)), Times.Once);
            result.Data!.TotalAmount.Should().Be(6 * 2m);
        }

        [Test]
        public async Task AddItemToCartAsync_AllowsQuantityEqualToStock()
        {
            var product = new Product{ Id = 8, Name = "S", Quantity = 5, PricePerSqm = 1m };
            _mockProductRepo.Setup(r => r.GetAsync(8)).ReturnsAsync(product);

            var cart = new Cart { Id = 80, SellerId = 8, CartItems = new List<CartItem>() };

            _mockCartRepo
                .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(cart)
                .ReturnsAsync(new Cart{ Id = 80, SellerId = 8, CartItems = new List<CartItem>{ new CartItem{ Id = 800, CartId = 80, ProductId = 8, Quantity = 5, Product = product } } });

            _mockCartItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<CartItem>()))
                .ReturnsAsync((CartItem ci) => ci);

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.AddItemToCartAsync(new AddCartItemDto{ ProductId = 8, Quantity = 5 }, 8);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data!.TotalAmount.Should().Be(5m);
        }
        #endregion

        #region UpdateCartItemAsync
        [Test]
        public async Task UpdateCartItemAsync_Unauthorized_WhenSellerNull()
        {
            var result = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 1 }, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task UpdateCartItemAsync_BadRequest_WhenQuantityNotPositive()
        {
            var result = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 0 }, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Quantity must be greater than 0");
        }

        [Test]
        public async Task UpdateCartItemAsync_NotFound_WhenCartItemMissing()
        {
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((CartItem?)null);

            var result = await _service.UpdateCartItemAsync(10, new UpdateCartItemDto{ Quantity = 1 }, 2);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Cart item not found");
        }

        [Test]
        public async Task UpdateCartItemAsync_Forbidden_WhenSellerMismatch()
        {
            var ci = new CartItem { Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 999 }, Product = new Product{ Id = 3 } };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            var result = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 2 }, 5);
            result.StatusCode.Should().Be(StatusCodes.Forbidden);
            result.Message.Should().Be("Unauthorized access");
        }

        [Test]
        public async Task UpdateCartItemAsync_NotFound_WhenProductMissingOrDeleted()
        {
            var ci = new CartItem { Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 5 } };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            _mockProductRepo.Setup(r => r.GetAsync(3)).ReturnsAsync((Product?)null);
            var result1 = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 2 }, 5);
            result1.StatusCode.Should().Be(StatusCodes.NotFound);

            _mockProductRepo.Setup(r => r.GetAsync(3)).ReturnsAsync(new Product{ Id = 3, IsDeleted = true });
            var result2 = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 2 }, 5);
            result2.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task UpdateCartItemAsync_BadRequest_WhenExceedsStock()
        {
            var ci = new CartItem { Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 5 } };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            _mockProductRepo.Setup(r => r.GetAsync(3)).ReturnsAsync(new Product{ Id = 3, Quantity = 4 });

            var result = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 5 }, 5);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Contain("Insufficient stock");
        }

        [Test]
        public async Task UpdateCartItemAsync_Success_WhenWithinStock()
        {
            var product = new Product{ Id = 3, Name = "Z", Quantity = 10, PricePerSqm = 4m };
            var ci = new CartItem { Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 5 }, Product = product };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            _mockProductRepo.Setup(r => r.GetAsync(3)).ReturnsAsync(product);

            _mockCartItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<CartItem>())).ReturnsAsync((CartItem x) => x);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // The subsequent GetCartAsync call returns a cart with the updated item
            _mockCartRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Cart{ Id = 2, SellerId = 5, CartItems = new List<CartItem>{ new CartItem{ Id = 1, CartId = 2, ProductId = 3, Quantity = 7, Product = product } } });

            var result = await _service.UpdateCartItemAsync(1, new UpdateCartItemDto{ Quantity = 7 }, 5);

            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockCartItemRepo.Verify(r => r.AddOrUpdateAsync(It.Is<CartItem>(x => x.Id == 1 && x.Quantity == 7)), Times.Once);
            result.Data!.TotalAmount.Should().Be(28m);
        }
        #endregion

        #region RemoveCartItemAsync
        [Test]
        public async Task RemoveCartItemAsync_Unauthorized_WhenSellerNull()
        {
            var result = await _service.RemoveCartItemAsync(1, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task RemoveCartItemAsync_NotFound_WhenMissing()
        {
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((CartItem?)null);

            var result = await _service.RemoveCartItemAsync(1, 2);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Cart item not found");
        }

        [Test]
        public async Task RemoveCartItemAsync_Forbidden_WhenNotOwner()
        {
            var ci = new CartItem{ Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 7 } };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            var result = await _service.RemoveCartItemAsync(1, 5);
            result.StatusCode.Should().Be(StatusCodes.Forbidden);
            result.Message.Should().Be("Unauthorized access");
        }

        [Test]
        public async Task RemoveCartItemAsync_Success()
        {
            var ci = new CartItem{ Id = 1, CartId = 2, ProductId = 3, Quantity = 1, Cart = new Cart{ Id = 2, SellerId = 5 } };
            _mockCartItemRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(ci);

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.RemoveCartItemAsync(1, 5);
            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockCartItemRepo.Verify(r => r.Delete(It.Is<CartItem>(x => x.Id == 1)), Times.Once);
        }
        #endregion

        #region ClearCartAsync
        [Test]
        public async Task ClearCartAsync_Unauthorized_WhenSellerNull()
        {
            var result = await _service.ClearCartAsync(null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task ClearCartAsync_NotFound_WhenCartMissing()
        {
            _mockCartRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Cart?)null);

            var result = await _service.ClearCartAsync(2);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Cart not found");
        }

        [Test]
        public async Task ClearCartAsync_Success_DeletesAllItems()
        {
            var cart = new Cart
            {
                Id = 99,
                SellerId = 1,
                CartItems = new List<CartItem>
                {
                    new CartItem{ Id = 900, CartId = 99, ProductId = 1, Quantity = 1 },
                    new CartItem{ Id = 901, CartId = 99, ProductId = 2, Quantity = 2 }
                }
            };

            _mockCartRepo
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(cart);

            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _service.ClearCartAsync(1);

            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockCartItemRepo.Verify(r => r.Delete(It.Is<CartItem>(x => x.Id == 900)), Times.Once);
            _mockCartItemRepo.Verify(r => r.Delete(It.Is<CartItem>(x => x.Id == 901)), Times.Once);
        }
        #endregion
    }
}
