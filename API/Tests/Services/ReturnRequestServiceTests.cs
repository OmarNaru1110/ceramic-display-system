using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Core.DTOs.ReturnRequest;
using Core.Services;
using Core.Services.IServices;
using CORE.DTOs;
using Data.DataAccess.Repositories.UnitOfWork;
using DATA.DataAccess.Repositories.IRepositories;
using Data.Models;
using Data.Models.Enums;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using StatusCodes = CORE.Constants.StatusCodes;

namespace Tests.Services
{
    [TestFixture]
    public class ReturnRequestServiceTests
    {
        private ReturnRequestService _service = null!;
        private Mock<IUnitOfWork> _uow = null!;
        private Mock<IBaseRepository<Order>> _orders = null!;
        private Mock<IBaseRepository<ReturnRequest>> _returnRequests = null!;
        private Mock<IBaseRepository<ReturnItem>> _returnItems = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IMapper> _mapper = null!;
        private Mock<Microsoft.Extensions.Logging.ILogger<ReturnRequestService>> _logger = null!;
        private Mock<IPdfGeneratorService> _pdf = null!;
        private Mock<IFileStorageService> _storage = null!;

        [SetUp]
        public void SetUp()
        {
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _orders = new Mock<IBaseRepository<Order>>(MockBehavior.Strict);
            _returnRequests = new Mock<IBaseRepository<ReturnRequest>>(MockBehavior.Strict);
            _returnItems = new Mock<IBaseRepository<ReturnItem>>(MockBehavior.Strict);
            _products = new Mock<IBaseRepository<Product>>(MockBehavior.Strict);
            _mapper = new Mock<IMapper>();
            _logger = new Mock<Microsoft.Extensions.Logging.ILogger<ReturnRequestService>>();
            _pdf = new Mock<IPdfGeneratorService>(MockBehavior.Strict);
            _storage = new Mock<IFileStorageService>(MockBehavior.Strict);

            _uow.SetupGet(x => x.Orders).Returns(_orders.Object);
            _uow.SetupGet(x => x.ReturnRequests).Returns(_returnRequests.Object);
            _uow.SetupGet(x => x.ReturnItems).Returns(_returnItems.Object);
            _uow.SetupGet(x => x.Products).Returns(_products.Object);

            _uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(async f => await f());
            _uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            _service = new ReturnRequestService(_uow.Object, _mapper.Object, _logger.Object, _pdf.Object, _storage.Object);
        }

        #region CreateReturnRequestAsync
        [Test]
        public async Task Create_Unauthorized_WhenSellerInvalid()
        {
            var dto = new CreateReturnRequestDto { OrderId = 1, ReturnReason = "rr", Items = new List<ReturnItemDto>() };
            (await _service.CreateReturnRequestAsync(dto, null)).StatusCode.Should().Be(StatusCodes.Unauthorized);
            (await _service.CreateReturnRequestAsync(dto, 0)).StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task Create_NotFound_WhenOrderMissing()
        {
            var dto = new CreateReturnRequestDto { OrderId = 10, ReturnReason = "rr", Items = new List<ReturnItemDto>() };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync((Order?)null);

            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task Create_Forbidden_WhenOrderOwnedByAnotherSeller()
        {
            var order = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Confirmed, OrderItems = new List<OrderItem>() };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync(order);

            var dto = new CreateReturnRequestDto { OrderId = 1, ReturnReason = "rr", Items = new List<ReturnItemDto>() };
            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.Forbidden);
        }

        [Test]
        public async Task Create_BadRequest_WhenOrderStatusInvalid()
        {
            var order = new Order { Id = 1, SellerId = 5, Status = OrderStatus.Pending, OrderItems = new List<OrderItem>() };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync(order);

            var dto = new CreateReturnRequestDto { OrderId = 1, ReturnReason = "rr", Items = new List<ReturnItemDto>() };
            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.BadRequest);
            res.Message.Should().Contain("confirmed or completed");
        }

        [Test]
        public async Task Create_BadRequest_WhenExistingReturnRequestFound()
        {
            var order = new Order { Id = 1, SellerId = 5, Status = OrderStatus.Confirmed, OrderItems = new List<OrderItem>() };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync(order);
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), (string[]?)null))
                           .ReturnsAsync(new ReturnRequest { Id = 7, OrderId = 1 });

            var dto = new CreateReturnRequestDto { OrderId = 1, ReturnReason = "rr", Items = new List<ReturnItemDto>() };
            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.BadRequest);
        }

        [Test]
        public async Task Create_BadRequest_WhenItemNotInOrderOrQuantityTooHigh()
        {
            var order = new Order
            {
                Id = 2,
                SellerId = 5,
                Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1 } }
            };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync(order);
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), (string[]?)null))
                           .ReturnsAsync((ReturnRequest?)null);

            // product 2 not in order
            var dtoMissing = new CreateReturnRequestDto
            {
                OrderId = 2,
                ReturnReason = "rr",
                Items = new List<ReturnItemDto> { new ReturnItemDto { ProductId = 2, Quantity = 1 } }
            };
            var res1 = await _service.CreateReturnRequestAsync(dtoMissing, 5);
            res1.StatusCode.Should().Be(StatusCodes.BadRequest);

            // quantity too high
            var dtoQty = new CreateReturnRequestDto
            {
                OrderId = 2,
                ReturnReason = "rr",
                Items = new List<ReturnItemDto> { new ReturnItemDto { ProductId = 1, Quantity = 5 } }
            };
            var res2 = await _service.CreateReturnRequestAsync(dtoQty, 5);
            res2.StatusCode.Should().Be(StatusCodes.BadRequest);
        }

        [Test]
        public async Task Create_Success_CreatesRequestAndItems_AndReturnsDto()
        {
            var order = new Order
            {
                Id = 3,
                SellerId = 5,
                Status = OrderStatus.Confirmed,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem{ ProductId = 10, Quantity = 3 },
                    new OrderItem{ ProductId = 11, Quantity = 2 }
                },
                CustomerName = "C",
                CustomerContact = "P",
                CustomerAddress = "A",
                TotalAmount = 100m,
                OrderDate = DateTime.UtcNow
            };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ReturnsAsync(order);

            // existing check -> null, later GetReturnRequestById -> provide entity
            _returnRequests.SetupSequence(r => r.FindAsync(
        It.IsAny<Expression<Func<ReturnRequest, bool>>>(),
        It.IsAny<string[]>()))
    .ReturnsAsync((ReturnRequest?)null)           // existing request check (includes = null)
    .ReturnsAsync(new ReturnRequest               // GetById call (includes = ["Order",...])
    {
        Id = 123,
        OrderId = 3,
        ReturnReason = "rr",
        Description = "d",
        Status = ReturnStatus.Pending,
        RequestDate = DateTime.UtcNow,
        Order = order,
        ReturnItems = new List<ReturnItem>
        {
            new ReturnItem{ Id = 1, ProductId = 10, Quantity = 1, Reason = "r1", Product = new Product{ Id = 10, Name = "P10" } },
            new ReturnItem{ Id = 2, ProductId = 11, Quantity = 2, Reason = "r2", Product = new Product{ Id = 11, Name = "P11" } }
        }
    });

            _returnRequests.Setup(r => r.AddOrUpdateAsync(It.IsAny<ReturnRequest>()))
                           .ReturnsAsync((ReturnRequest rr) => { rr.Id = 123; return rr; });
            _returnItems.Setup(r => r.AddOrUpdateAsync(It.IsAny<ReturnItem>()))
                        .ReturnsAsync((ReturnItem ri) => ri);

            var dto = new CreateReturnRequestDto
            {
                OrderId = 3,
                ReturnReason = "rr",
                Description = "d",
                Items = new List<ReturnItemDto>
                {
                    new ReturnItemDto{ ProductId = 10, Quantity = 1, Reason = "r1" },
                    new ReturnItemDto{ ProductId = 11, Quantity = 2, Reason = "r2" }
                }
            };

            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.OK);
            res.Data.Should().NotBeNull();
            res.Data!.Items.Should().HaveCount(2);

            _returnRequests.Verify(r => r.AddOrUpdateAsync(It.IsAny<ReturnRequest>()), Times.Once);
            _returnItems.Verify(r => r.AddOrUpdateAsync(It.IsAny<ReturnItem>()), Times.Exactly(2));
            _uow.Verify(u => u.CommitAsync(), Times.AtLeast(2));
        }

        [Test]
        public async Task Create_InternalError_OnUnexpectedException()
        {
            var dto = new CreateReturnRequestDto { OrderId = 1, ReturnReason = "x", Items = new List<ReturnItemDto>() };
            _orders.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>() ))
                   .ThrowsAsync(new Exception("db error"));

            var res = await _service.CreateReturnRequestAsync(dto, 5);
            res.StatusCode.Should().Be(StatusCodes.InternalServerError);
        }
        #endregion

        #region GetReturnRequestByIdAsync
        [Test]
        public async Task GetById_NotFound_Forbidden_And_Success()
        {
            // Not found
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync((ReturnRequest?)null);
            var nf = await _service.GetReturnRequestByIdAsync(1, 10, new[] { "Seller" });
            nf.StatusCode.Should().Be(StatusCodes.NotFound);

            // Forbidden when not admin and wrong seller
            var rr = new ReturnRequest
            {
                Id = 1,
                OrderId = 9,
                Status = ReturnStatus.Pending,
                Order = new Order { Id = 9, SellerId = 99, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow },
                ReturnItems = new List<ReturnItem>()
            };
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync(rr);
            var forb = await _service.GetReturnRequestByIdAsync(1, 10, new[] { "Seller" });
            forb.StatusCode.Should().Be(StatusCodes.Forbidden);

            // Allowed for seller
            rr.Order.SellerId = 10;
            var okSeller = await _service.GetReturnRequestByIdAsync(1, 10, new[] { "Seller" });
            okSeller.StatusCode.Should().Be(StatusCodes.OK);

            // Allowed for admin regardless
            rr.Order.SellerId = 999;
            var okAdmin = await _service.GetReturnRequestByIdAsync(1, 10, new[] { "Admin" });
            okAdmin.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region GetReturnRequestsAsync
        [Test]
        public async Task GetList_ForSeller_And_AdminWithFilters()
        {
            var filter = new ReturnRequestFilterDto { PageNumber = 1, PageSize = 10 };

            _returnRequests.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<bool>()))
                           .ReturnsAsync(2);
            _returnRequests.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<ReturnRequest, bool>>>(),
                filter.PageNumber,
                filter.PageSize,
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<ReturnRequest, object>>>(),
                It.IsAny<DATA.Constants.Enums.OrderBy>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<ReturnRequest>
            {
                new ReturnRequest{ Id = 1, OrderId = 5, Status = ReturnStatus.Pending, RequestDate = DateTime.UtcNow, Order = new Order{ Id=5, SellerId = 7, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow }, ReturnItems = new List<ReturnItem>() },
                new ReturnRequest{ Id = 2, OrderId = 5, Status = ReturnStatus.Accepted, RequestDate = DateTime.UtcNow, Order = new Order{ Id=5, SellerId = 7, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow }, ReturnItems = new List<ReturnItem>() }
            });

            var userRes = await _service.GetReturnRequestsAsync(filter, 7, new[] { "Seller" });
            userRes.StatusCode.Should().Be(StatusCodes.OK);
            userRes.Data!.Items.Should().HaveCount(2);
            userRes.Data.TotalCount.Should().Be(2);

            // Admin path with SellerId filter
            filter.SellerId = 99;
            var adminRes = await _service.GetReturnRequestsAsync(filter, 1, new[] { "Admin" });
            adminRes.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region AcceptReturnRequestAsync
        [Test]
        public async Task Accept_Unauthorized_NotFound_BadRequest()
        {
            (await _service.AcceptReturnRequestAsync(1, null, null)).StatusCode.Should().Be(StatusCodes.Unauthorized);
            (await _service.AcceptReturnRequestAsync(1, 0, null)).StatusCode.Should().Be(StatusCodes.Unauthorized);

            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync((ReturnRequest?)null);
            var nf = await _service.AcceptReturnRequestAsync(1, 1, null);
            nf.StatusCode.Should().Be(StatusCodes.NotFound);

            var nonPending = new ReturnRequest { Id = 2, Status = ReturnStatus.Accepted, Order = new Order { Id = 1, SellerId = 1, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow }, ReturnItems = new List<ReturnItem>() };
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync(nonPending);
            var bad = await _service.AcceptReturnRequestAsync(2, 1, null);
            bad.StatusCode.Should().Be(StatusCodes.BadRequest);
        }

        [Test]
        public async Task Accept_Success_Restocks_GeneratesPdf_AndReturnsDto()
        {
            var itemWithProduct = new ReturnItem { Id = 1, ProductId = 10, Quantity = 3, Product = new Product { Id = 10, Name = "P10", Quantity = 5 } };
            var itemWithoutProduct = new ReturnItem { Id = 2, ProductId = 11, Quantity = 2, Product = null! };
            var pending = new ReturnRequest
            {
                Id = 5,
                Status = ReturnStatus.Pending,
                OrderId = 100,
                Order = new Order { Id = 100, SellerId = 9, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow },
                ReturnItems = new List<ReturnItem> { itemWithProduct, itemWithoutProduct }
            };
            var accepted = new ReturnRequest
            {
                Id = 5,
                Status = ReturnStatus.Accepted,
                OrderId = 100,
                Order = pending.Order,
                ReturnItems = pending.ReturnItems
            };

            _returnRequests.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync(pending) // initial fetch
                           .ReturnsAsync(accepted); // final GetById

            _returnRequests.Setup(r => r.AddOrUpdateAsync(It.IsAny<ReturnRequest>()))
                           .ReturnsAsync((ReturnRequest rr) => rr);

            _products.Setup(r => r.AddOrUpdateAsync(It.IsAny<Product>()))
                     .ReturnsAsync((Product p) => p);
            _products.Setup(r => r.GetAsync(11)).ReturnsAsync(new Product { Id = 11, Name = "P11", Quantity = 1 });

            _pdf.Setup(p => p.GenerateReturnRequestPdfAsync(It.IsAny<ReturnRequest>(), It.IsAny<Order>(), It.IsAny<List<ReturnItem>>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _storage.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.Is<string>(n => n.StartsWith("return-")), "returns", "application/pdf"))
                    .ReturnsAsync("https://files/return-5.pdf");

            var res = await _service.AcceptReturnRequestAsync(5, 1, "ok");

            res.StatusCode.Should().Be(StatusCodes.OK);
            _products.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(p => p.Id == 10 && p.Quantity == 8)), Times.Once); // 5 + 3
            _products.Verify(r => r.AddOrUpdateAsync(It.Is<Product>(p => p.Id == 11 && p.Quantity == 3)), Times.Once); // 1 + 2
            _pdf.Verify(p => p.GenerateReturnRequestPdfAsync(It.IsAny<ReturnRequest>(), It.IsAny<Order>(), It.IsAny<List<ReturnItem>>()), Times.Once);
            _storage.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.Is<string>(x => x.Contains("5")), "returns", "application/pdf"), Times.Once);
            _returnRequests.Verify(r => r.AddOrUpdateAsync(It.Is<ReturnRequest>(x => x.Status == ReturnStatus.Accepted)), Times.AtLeastOnce);
            _uow.Verify(u => u.CommitAsync(), Times.AtLeast(2));
        }

        [Test]
        public async Task Accept_Continues_WhenPdfGenerationFails()
        {
            var pending = new ReturnRequest
            {
                Id = 6,
                Status = ReturnStatus.Pending,
                OrderId = 101,
                Order = new Order { Id = 101, SellerId = 9, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow },
                ReturnItems = new List<ReturnItem>()
            };
            var accepted = new ReturnRequest { Id = 6, Status = ReturnStatus.Accepted, OrderId = 101, Order = pending.Order, ReturnItems = pending.ReturnItems };

            _returnRequests.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync(pending)
                           .ReturnsAsync(accepted);
            _returnRequests.Setup(r => r.AddOrUpdateAsync(It.IsAny<ReturnRequest>()))
                           .ReturnsAsync((ReturnRequest rr) => rr);

            // No restock needed (no items), commit still called
            _pdf.Setup(p => p.GenerateReturnRequestPdfAsync(It.IsAny<ReturnRequest>(), It.IsAny<Order>(), It.IsAny<List<ReturnItem>>()))
                .ThrowsAsync(new Exception("pdf error"));

            var res = await _service.AcceptReturnRequestAsync(6, 1, null);

            res.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region RejectReturnRequestAsync
        [Test]
        public async Task Reject_Unauthorized_NotFound_BadRequest_And_Success()
        {
            (await _service.RejectReturnRequestAsync(1, null, null)).StatusCode.Should().Be(StatusCodes.Unauthorized);

            _returnRequests.Setup(r => r.GetAsync(2)).ReturnsAsync((ReturnRequest?)null);
            var nf = await _service.RejectReturnRequestAsync(2, 1, null);
            nf.StatusCode.Should().Be(StatusCodes.NotFound);

            _returnRequests.Setup(r => r.GetAsync(3)).ReturnsAsync(new ReturnRequest { Id = 3, Status = ReturnStatus.Accepted });
            var br = await _service.RejectReturnRequestAsync(3, 1, null);
            br.StatusCode.Should().Be(StatusCodes.BadRequest);

            var pending = new ReturnRequest { Id = 4, Status = ReturnStatus.Pending, OrderId = 9 };
            _returnRequests.Setup(r => r.GetAsync(4)).ReturnsAsync(pending);
            _returnRequests.Setup(r => r.AddOrUpdateAsync(It.IsAny<ReturnRequest>()))
                           .ReturnsAsync((ReturnRequest rr) => rr);

            // For final GetById called inside service
            _returnRequests.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<string[]>() ))
                           .ReturnsAsync(new ReturnRequest
                           {
                               Id = 4,
                               Status = ReturnStatus.Rejected,
                               OrderId = 9,
                               Order = new Order { Id = 9, SellerId = 1, CustomerName = "C", CustomerAddress = "A", CustomerContact = "P", OrderDate = DateTime.UtcNow },
                               ReturnItems = new List<ReturnItem>()
                           });

            var ok = await _service.RejectReturnRequestAsync(4, 1, "bad");
            ok.StatusCode.Should().Be(StatusCodes.OK);
            _returnRequests.Verify(r => r.AddOrUpdateAsync(It.Is<ReturnRequest>(x => x.Status == ReturnStatus.Rejected && x.AdminNotes == "bad")), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }
        #endregion
    }
}
