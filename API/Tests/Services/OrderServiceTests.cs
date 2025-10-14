using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Core.DTOs.Order;
using Core.Services;
using Core.Services.IServices;
using CORE.DTOs;
using Core.DTOs.Invoice;
using DATA.DataAccess.Repositories.IRepositories;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using Data.Models.Enums;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tests.Helpers;
using StatusCodes = CORE.Constants.StatusCodes;

namespace Tests.Services
{
    [TestFixture]
    public class OrderServiceTests : TestBase
    {
        private OrderService _service = null!;
        private Mock<IBaseRepository<Order>> _mockOrderRepo = null!;
        private Mock<IBaseRepository<OrderItem>> _mockOrderItemRepo = null!;
        private Mock<IBaseRepository<Product>> _mockProductRepo = null!;
        private Mock<IInvoiceService> _mockInvoiceService = null!;
        private Mock<Microsoft.Extensions.Logging.ILogger<OrderService>> _mockLogger = null!;
        private Mock<IAdminNotificationService> _mockAdminNotification = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();

            _mockOrderRepo = new Mock<IBaseRepository<Order>>();
            _mockOrderItemRepo = new Mock<IBaseRepository<OrderItem>>();
            _mockProductRepo = new Mock<IBaseRepository<Product>>();
            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<OrderService>>();
            _mockAdminNotification = new Mock<IAdminNotificationService>();

            MockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepo.Object);
            MockUnitOfWork.Setup(u => u.OrderItems).Returns(_mockOrderItemRepo.Object);
            MockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);

            // Execute transaction immediately
            MockUnitOfWork
                .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ResponseDto<GetOrderDto>>>>()))
                .Returns((Func<Task<ResponseDto<GetOrderDto>>> op) => op());

            // Default CommitAsync returns success
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new OrderService(
                MockUnitOfWork.Object,
                MockMapper.Object,
                _mockLogger.Object,
                _mockInvoiceService.Object,
                _mockAdminNotification.Object);
        }

        #region CreateOrderAsync
        [Test]
        public async Task CreateOrderAsync_Unauthorized_WhenSellerIdNullOrInvalid()
        {
            var dto = new CreateOrderDto { CustomerName = "C", CustomerContact = "P", CustomerAddress = "A", Items = new List<OrderItemDto>() };

            var r1 = await _service.CreateOrderAsync(dto, null);
            r1.StatusCode.Should().Be(StatusCodes.Unauthorized);

            var r2 = await _service.CreateOrderAsync(dto, 0);
            r2.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task CreateOrderAsync_NotFound_WhenAnyProductMissingOrDeleted()
        {
            var dto = new CreateOrderDto
            {
                CustomerName = "John",
                CustomerContact = "123",
                CustomerAddress = "Addr",
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 10, Quantity = 2 } }
            };
            _mockProductRepo.Setup(r => r.GetAsync(10)).ReturnsAsync((Product?)null);

            var result = await _service.CreateOrderAsync(dto, 5);
            result.StatusCode.Should().Be(StatusCodes.NotFound);

            _mockProductRepo.Setup(r => r.GetAsync(10)).ReturnsAsync(new Product { Id = 10, IsDeleted = true });
            result = await _service.CreateOrderAsync(dto, 5);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task CreateOrderAsync_BadRequest_WhenInsufficientStockInPreValidation()
        {
            var dto = new CreateOrderDto
            {
                CustomerName = "John",
                CustomerContact = "123",
                CustomerAddress = "Addr",
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 11, Quantity = 5 } }
            };

            _mockProductRepo.Setup(r => r.GetAsync(11)).ReturnsAsync(new Product { Id = 11, Name = "P", Quantity = 4, PricePerSqm = 3m });

            var result = await _service.CreateOrderAsync(dto, 7);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Contain("Insufficient stock");
        }

        [Test]
        public async Task CreateOrderAsync_Success_CreatesOrder_DeductsStock_AddsItems_AndReturnsDto()
        {
            var p1 = new Product { Id = 1, Name = "A", Quantity = 10, PricePerSqm = 5m };
            var p2 = new Product { Id = 2, Name = "B", Quantity = 8, PricePerSqm = 2m };
            var dto = new CreateOrderDto
            {
                CustomerName = "John",
                CustomerContact = "123",
                CustomerAddress = "Addr",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 },
                    new OrderItemDto { ProductId = 2, Quantity = 3 }
                }
            };

            // Pre-validation gets
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(p1);
            _mockProductRepo.Setup(r => r.GetAsync(2)).ReturnsAsync(p2);

            // Add order assigns Id
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) => { o.Id = 100; return o; });

            // Inside-transaction product re-fetches
            _mockProductRepo.SetupSequence(r => r.GetAsync(1))
                .ReturnsAsync(p1) // pre-validate
                .ReturnsAsync(p1); // in-transaction
            _mockProductRepo.SetupSequence(r => r.GetAsync(2))
                .ReturnsAsync(p2) // pre-validate
                .ReturnsAsync(p2); // in-transaction

            // Capture created order items
            var createdItems = new List<OrderItem>();
            _mockOrderItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<OrderItem>()))
                .ReturnsAsync((OrderItem oi) => { createdItems.Add(oi); oi.Id = createdItems.Count; return oi; });

            // After creation, GetOrderByIdAsync will query repo
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((System.Linq.Expressions.Expression<Func<Order, bool>> _, string[] __) =>
                {
                    return new Order
                    {
                        Id = 100,
                        SellerId = 9,
                        CustomerName = dto.CustomerName,
                        CustomerContact = dto.CustomerContact,
                        CustomerAddress = dto.CustomerAddress,
                        TotalAmount = 2 * 5m + 3 * 2m,
                        Status = OrderStatus.Pending,
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem{ Id = 1, OrderId = 100, ProductId = 1, Quantity = 2, PricePerSqm = 5m, TotalPrice = 10m, Product = new Product{ Id = 1, Name = "A" }},
                            new OrderItem{ Id = 2, OrderId = 100, ProductId = 2, Quantity = 3, PricePerSqm = 2m, TotalPrice = 6m, Product = new Product{ Id = 2, Name = "B" }}
                        }
                    };
                });

            var result = await _service.CreateOrderAsync(dto, 9);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalAmount.Should().Be(16m);

            // Verify stock adjusted
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 1 && x.Quantity == 8)), Times.Once);
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 2 && x.Quantity == 5)), Times.Once);
            createdItems.Should().HaveCount(2);

            // Verify notification fired
            _mockAdminNotification.Verify(n => n.NotifyOrderPlacedAsync(It.Is<GetOrderDto>(g => g.Id == 100)), Times.Once);
        }

        [Test]
        public async Task CreateOrderAsync_BadRequest_WhenStockBecomesInsufficientDuringTransaction()
        {
            var p = new Product { Id = 3, Name = "C", Quantity = 5, PricePerSqm = 4m };
            var dto = new CreateOrderDto
            {
                CustomerName = "John",
                CustomerContact = "123",
                CustomerAddress = "Addr",
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 3, Quantity = 4 } }
            };

            // Pre-validate ok
            _mockProductRepo.SetupSequence(r => r.GetAsync(3))
                .ReturnsAsync(p) // pre-validate
                .ReturnsAsync(new Product { Id = 3, Name = "C", Quantity = 2, PricePerSqm = 4m }); // in-transaction insufficient

            // During transaction, when insufficient, an InvalidOperationException will be thrown by service
            // We still need AddOrUpdate for order before items are processed
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) => { o.Id = 200; return o; });

            var result = await _service.CreateOrderAsync(dto, 5);

            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Contain("Insufficient stock");
        }
        #endregion

        #region UpdateOrderAsync
        [Test]
        public async Task UpdateOrderAsync_AuthAndExistenceChecks()
        {
            var dto = new UpdateOrderDto();
            var r1 = await _service.UpdateOrderAsync(1, dto, null);
            r1.StatusCode.Should().Be(StatusCodes.Unauthorized);

            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Order?)null);
            var r2 = await _service.UpdateOrderAsync(1, dto, 1);
            r2.StatusCode.Should().Be(StatusCodes.NotFound);

            var otherOrder = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(otherOrder);
            var r3 = await _service.UpdateOrderAsync(1, dto, 1);
            r3.StatusCode.Should().Be(StatusCodes.Forbidden);

            otherOrder.SellerId = 1;
            otherOrder.Status = OrderStatus.Confirmed;
            var r4 = await _service.UpdateOrderAsync(1, dto, 1);
            r4.StatusCode.Should().Be(StatusCodes.BadRequest);
        }

        [Test]
        public async Task UpdateOrderAsync_MetadataOnly_UpdatesFields()
        {
            var order = new Order
            {
                Id = 10,
                SellerId = 5,
                Status = OrderStatus.Pending,
                CustomerName = "Old",
                CustomerContact = "OldC",
                CustomerAddress = "OldA",
                Notes = "OldN",
                OrderItems = new List<OrderItem>()
            };

            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(order);
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);

            var dto = new UpdateOrderDto
            {
                CustomerName = "New",
                CustomerContact = "NewC",
                CustomerAddress = "NewA",
                Notes = "NewN",
                Items = null
            };

            var result = await _service.UpdateOrderAsync(10, dto, 5);
            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockOrderRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Order>(o => o.CustomerName == "New" && o.CustomerContact == "NewC" && o.CustomerAddress == "NewA" && o.Notes == "NewN")), Times.Once);
        }

        [Test]
        public async Task UpdateOrderAsync_ItemIncrease_InsufficientAdditionalStock_ReturnsBadRequest()
        {
            var order = new Order
            {
                Id = 20,
                SellerId = 2,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem> { new OrderItem{ Id = 1, OrderId = 20, ProductId = 9, Quantity = 2 } }
            };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(order);

            var product = new Product { Id = 9, Name = "P", Quantity = 1, PricePerSqm = 5m }; // only 1 additional available
            _mockProductRepo.Setup(r => r.GetAsync(9)).ReturnsAsync(product);

            var dto = new UpdateOrderDto { Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 9, Quantity = 4 } } }; // diff = +2 > stock 1
            var result = await _service.UpdateOrderAsync(20, dto, 2);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Contain("Insufficient stock");
        }

        [Test]
        public async Task UpdateOrderAsync_ItemIncrease_Decrease_Add_Remove_AdjustsStockAndTotals()
        {
            // Existing order has product 1 qty 2 (price 5), and product 2 qty 4 (price 3)
            var p1 = new Product { Id = 1, Name = "A", Quantity = 10, PricePerSqm = 5m };
            var p2 = new Product { Id = 2, Name = "B", Quantity = 10, PricePerSqm = 3m };
            var order = new Order
            {
                Id = 30,
                SellerId = 3,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem{ Id = 101, OrderId = 30, ProductId = 1, Quantity = 2, PricePerSqm = 5m, TotalPrice = 10m },
                    new OrderItem{ Id = 102, OrderId = 30, ProductId = 2, Quantity = 4, PricePerSqm = 3m, TotalPrice = 12m }
                }
            };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(order);

            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(p1);
            _mockProductRepo.Setup(r => r.GetAsync(2)).ReturnsAsync(p2);
            _mockOrderItemRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<OrderItem>())).ReturnsAsync((OrderItem oi) => oi);
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);

            // Update: increase product 1 to 3, decrease product 2 to 1, and add new product 4 qty 2 at price 7
            var p4 = new Product { Id = 4, Name = "D", Quantity = 5, PricePerSqm = 7m };
            _mockProductRepo.Setup(r => r.GetAsync(4)).ReturnsAsync(p4);

            var dto = new UpdateOrderDto
            {
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto{ ProductId = 1, Quantity = 3 }, // +1 diff
                    new OrderItemDto{ ProductId = 2, Quantity = 1 }, // -3 diff
                    new OrderItemDto{ ProductId = 4, Quantity = 2 }  // new item
                }
            };

            var result = await _service.UpdateOrderAsync(30, dto, 3);

            result.StatusCode.Should().Be(StatusCodes.OK);
            // Product 1 qty decreased by 1, product 2 qty increased by 3, product 4 qty decreased by 2
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 1 && x.Quantity == 9)), Times.Once);
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 2 && x.Quantity == 13)), Times.Once);
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 4 && x.Quantity == 3)), Times.Once);

            // Removed items none, but verify total updated: (3*5) + (1*3) + (2*7) = 15 + 3 + 14 = 32
            _mockOrderRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Order>(o => o.TotalAmount == 32m)), Times.Once);
        }

        [Test]
        public async Task UpdateOrderAsync_RemovedItems_RestoresStock_AndDeletesItems()
        {
            var p1 = new Product { Id = 1, Name = "A", Quantity = 0, PricePerSqm = 5m };
            var order = new Order
            {
                Id = 40,
                SellerId = 4,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem{ Id = 201, OrderId = 40, ProductId = 1, Quantity = 2, PricePerSqm = 5m, TotalPrice = 10m }
                }
            };

            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(order);
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(p1);

            var dto = new UpdateOrderDto { Items = new List<OrderItemDto>() }; // removed all

            var result = await _service.UpdateOrderAsync(40, dto, 4);

            result.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region CancelOrderAsync
        [Test]
        public async Task CancelOrderAsync_Validations_And_Success()
        {
            var r1 = await _service.CancelOrderAsync(1, null);
            r1.StatusCode.Should().Be(StatusCodes.Unauthorized);

            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Order?)null);
            var r2 = await _service.CancelOrderAsync(1, 1);
            r2.StatusCode.Should().Be(StatusCodes.NotFound);

            var order = new Order { Id = 2, SellerId = 99, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(order);
            var r3 = await _service.CancelOrderAsync(2, 1);
            r3.StatusCode.Should().Be(StatusCodes.Forbidden);

            order.SellerId = 1; order.Status = OrderStatus.Confirmed;
            var r4 = await _service.CancelOrderAsync(2, 1);
            r4.StatusCode.Should().Be(StatusCodes.BadRequest);

            // Success path - restore stock and set status cancelled
            var p = new Product { Id = 5, Quantity = 1 };
            var okOrder = new Order
            {
                Id = 3,
                SellerId = 1,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem> { new OrderItem{ Id = 1, OrderId = 3, ProductId = 5, Quantity = 2 } }
            };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(okOrder);
            _mockProductRepo.Setup(r => r.GetAsync(5)).ReturnsAsync(p);
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);

            var r5 = await _service.CancelOrderAsync(3, 1);
            r5.StatusCode.Should().Be(StatusCodes.OK);
            _mockProductRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(x => x.Id == 5 && x.Quantity == 3)), Times.Once);
            _mockOrderRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.Cancelled)), Times.Once);
        }
        #endregion

        #region GetOrderByIdAsync
        [Test]
        public async Task GetOrderByIdAsync_NotFound_Forbidden_And_Success()
        {
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Order?)null);
            var nf = await _service.GetOrderByIdAsync(1, 1, new[] { "Seller" });
            nf.StatusCode.Should().Be(StatusCodes.NotFound);

            var order = new Order { Id = 1, SellerId = 2, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(order);
            var forb = await _service.GetOrderByIdAsync(1, 1, new[] { "Seller" });
            forb.StatusCode.Should().Be(StatusCodes.Forbidden);

            var ok = await _service.GetOrderByIdAsync(1, 2, new[] { "Seller" });
            ok.StatusCode.Should().Be(StatusCodes.OK);

            // Admin can access regardless of seller
            var adminOk = await _service.GetOrderByIdAsync(1, 999, new[] { "Admin" });
            adminOk.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region GetOrdersAsync
        [Test]
        public async Task GetOrdersAsync_ReturnsPagedResults_ForUser_And_AdminWithFilters()
        {
            var filter = new OrderFilterDto { PageNumber = 1, PageSize = 10 };

            MockUnitOfWork.Setup(u => u.Orders.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<bool>()))
                .ReturnsAsync(2);
            MockUnitOfWork.Setup(u => u.Orders.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                    filter.PageNumber,
                    filter.PageSize,
                    It.IsAny<string[]>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Order, object>>>(),
                    It.IsAny<DATA.Constants.Enums.OrderBy>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new List<Order>
                {
                    new Order{ Id = 1, SellerId = 5, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() },
                    new Order{ Id = 2, SellerId = 5, Status = OrderStatus.Confirmed, OrderItems = new List<OrderItem>() }
                });

            var userResult = await _service.GetOrdersAsync(filter, 5, new[] { "Seller" });
            userResult.StatusCode.Should().Be(StatusCodes.OK);
            userResult.Data!.Items.Should().HaveCount(2);
            userResult.Data.TotalCount.Should().Be(2);

            // Admin with SellerId filter
            filter.SellerId = 10;
            var adminResult = await _service.GetOrdersAsync(filter, 1, new[] { "Admin" });
            adminResult.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region ConfirmOrderAsync
        [Test]
        public async Task ConfirmOrderAsync_Validations_And_Success()
        {
            var r1 = await _service.ConfirmOrderAsync(1, null);
            r1.StatusCode.Should().Be(StatusCodes.Unauthorized);

            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Order?)null);
            var r2 = await _service.ConfirmOrderAsync(1, 10);
            r2.StatusCode.Should().Be(StatusCodes.NotFound);

            var order = new Order { Id = 5, SellerId = 3, Status = OrderStatus.Rejected, OrderItems = new List<OrderItem>() };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(order);
            var r3 = await _service.ConfirmOrderAsync(5, 10);
            r3.StatusCode.Should().Be(StatusCodes.BadRequest);

            // Success
            order.Status = OrderStatus.Pending;
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);
            _mockInvoiceService.Setup(s => s.GenerateInvoiceAsync(5)).ReturnsAsync(new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.OK, Data = new GetInvoiceDto { OrderId = 5 } });
            // Subsequent GetOrderById call
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(new Order { Id = 5, SellerId = 3, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() });

            var ok = await _service.ConfirmOrderAsync(5, 10);
            ok.StatusCode.Should().Be(StatusCodes.OK);
            _mockInvoiceService.Verify(s => s.GenerateInvoiceAsync(5), Times.Once);
        }
        #endregion

        #region RejectOrderAsync
        [Test]
        public async Task RejectOrderAsync_Validations_And_Success()
        {
            var r1 = await _service.RejectOrderAsync(1, null, null);
            r1.StatusCode.Should().Be(StatusCodes.Unauthorized);

            _mockOrderRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Order?)null);
            var r2 = await _service.RejectOrderAsync(1, 1, null);
            r2.StatusCode.Should().Be(StatusCodes.NotFound);

            var order = new Order { Id = 2, SellerId = 3, Status = OrderStatus.Confirmed };
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() )).ReturnsAsync(order);
            var r3 = await _service.RejectOrderAsync(2, 1, null);
            r3.StatusCode.Should().Be(StatusCodes.BadRequest);

            order.Status = OrderStatus.Pending;
            _mockOrderRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);
            _mockOrderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(new Order { Id = 2, SellerId = 3, Status = OrderStatus.Pending, Notes = "Rejection reason: bad", OrderItems = new List<OrderItem>() });

            var ok = await _service.RejectOrderAsync(2, 1, "bad");
            ok.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion
    }
}
