using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Core.DTOs.Invoice;
using Core.Services;
using Core.Services.IServices;
using CORE.DTOs;
using DATA.DataAccess.Repositories.IRepositories;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tests.Helpers;
using StatusCodes = CORE.Constants.StatusCodes;

namespace Tests.Services
{
    [TestFixture]
    public class InvoiceServiceTests : TestBase
    {
        private InvoiceService _service = null!;
        private Mock<IBaseRepository<Order>> _mockOrderRepo = null!;
        private Mock<IBaseRepository<Invoice>> _mockInvoiceRepo = null!;
        private Mock<IPdfGeneratorService> _mockPdfGenerator = null!;
        private Mock<IFileStorageService> _mockFileStorage = null!;
        private Mock<Microsoft.Extensions.Logging.ILogger<InvoiceService>> _mockLogger = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();

            _mockOrderRepo = new Mock<IBaseRepository<Order>>();
            _mockInvoiceRepo = new Mock<IBaseRepository<Invoice>>();
            _mockPdfGenerator = new Mock<IPdfGeneratorService>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<InvoiceService>>();

            MockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepo.Object);
            MockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepo.Object);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new InvoiceService(MockUnitOfWork.Object, MockMapper.Object, _mockLogger.Object, _mockPdfGenerator.Object, _mockFileStorage.Object);
        }

        private static Order MakeOrder(int id = 1, int sellerId = 99, decimal total = 123.45m)
        {
            return new Order
            {
                Id = id,
                SellerId = sellerId,
                CustomerName = "Cust",
                CustomerContact = "Phone",
                CustomerAddress = "Addr",
                TotalAmount = total,
                OrderDate = DateTime.UtcNow,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem{ Id = 10, OrderId = id, ProductId = 5, Quantity = 2, PricePerSqm = 10, TotalPrice = 20 }
                }
            };
        }

        private static Invoice MakeInvoice(int id = 1, int orderId = 1, int sellerId = 99, string? pdfUrl = null, decimal total = 123.45m)
        {
            return new Invoice
            {
                Id = id,
                OrderId = orderId,
                TotalAmount = total,
                InvoiceDate = DateTime.UtcNow,
                PdfUrl = pdfUrl,
                Order = MakeOrder(orderId, sellerId, total)
            };
        }

        private static Invoice MakeInvoiceForCriteria(int sellerId, int orderId, DateTime date)
        {
            return new Invoice
            {
                Id = 0,
                OrderId = orderId,
                TotalAmount = 0,
                InvoiceDate = date,
                Order = new Order { Id = orderId, SellerId = sellerId, CustomerName = "C", CustomerContact = "P", CustomerAddress = "A", TotalAmount = 0, OrderDate = DateTime.UtcNow, OrderItems = new List<OrderItem>() }
            };
        }

        #region GenerateInvoiceAsync
        [Test]
        public async Task GenerateInvoiceAsync_OrderNotFound_ReturnsNotFound()
        {
            _mockOrderRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>()));

            var result = await _service.GenerateInvoiceAsync(1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Order not found");
        }

        [Test]
        public async Task GenerateInvoiceAsync_InvoiceAlreadyExists_ReturnsBadRequest()
        {
            var order = MakeOrder(5, 7);
            _mockOrderRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(order);

            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(MakeInvoice(2, order.Id, order.SellerId));

            var result = await _service.GenerateInvoiceAsync(order.Id);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Invoice already exists");
        }

        [Test]
        public async Task GenerateInvoiceAsync_Success_GeneratesPdfAndReturnsDto()
        {
            var order = MakeOrder(11, 22, 77.7m);
            _mockOrderRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(order);

            // Keep track of the current invoice state across AddOrUpdateAsync calls
            Invoice? currentInvoice = null;

            // First check for existing invoice returns null
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Expression<Func<Invoice, bool>> _, string[]? __) => (Invoice?)null);

            _mockInvoiceRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice inv) =>
                {
                    if (inv.Id == 0) inv.Id = 123;
                    currentInvoice = inv;
                    return inv;
                });

            var pdfBytes = new byte[] { 1, 2, 3 };
            _mockPdfGenerator
                .Setup(p => p.GenerateInvoicePdfAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<List<OrderItem>>()))
                .ReturnsAsync(pdfBytes);

            var pdfUrl = "http://files/invoices/invoice-123.pdf";
            _mockFileStorage
                .Setup(s => s.UploadFileAsync(pdfBytes, "invoice-123.pdf", "invoices", "application/pdf"))
                .ReturnsAsync(pdfUrl);

            // When GetInvoiceByIdAsync runs, it loads invoice with Order
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Invoice, bool>>>(),
                    It.Is<string[]>(inc => inc != null && inc.Contains("Order"))))
                .ReturnsAsync(() =>
                {
                    if (currentInvoice != null && currentInvoice.Order == null)
                        currentInvoice.Order = order;
                    return currentInvoice!;
                });

            var result = await _service.GenerateInvoiceAsync(order.Id);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.OrderId.Should().Be(order.Id);
            result.Data.TotalAmount.Should().Be(order.TotalAmount);
            result.Data.PdfUrl.Should().Be(pdfUrl);

            _mockPdfGenerator.Verify(p => p.GenerateInvoicePdfAsync(It.IsAny<Invoice>(), order, It.IsAny<List<OrderItem>>()), Times.Once);
            _mockFileStorage.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.Is<string>(f => f.StartsWith("invoice-")), "invoices", "application/pdf"), Times.Once);
            _mockInvoiceRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<Invoice>()), Times.AtLeast(2));
            MockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeast(2));
        }

        [Test]
        public async Task GenerateInvoiceAsync_PdfGenerationFails_StillReturnsDtoWithoutPdf()
        {
            var order = MakeOrder(21, 22);
            _mockOrderRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(order);

            Invoice? currentInvoice = null;

            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync((Expression<Func<Invoice, bool>> _, string[]? __) => (Invoice?)null);

            _mockInvoiceRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice inv) => { if (inv.Id == 0) inv.Id = 200; currentInvoice = inv; return inv; });

            _mockPdfGenerator
                .Setup(p => p.GenerateInvoicePdfAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<List<OrderItem>>()))
                .ThrowsAsync(new Exception("PDF error"));

            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(() => { if (currentInvoice != null && currentInvoice.Order == null) currentInvoice.Order = order; return currentInvoice!; });

            var result = await _service.GenerateInvoiceAsync(order.Id);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.PdfUrl.Should().BeNull();
            _mockFileStorage.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region GetInvoicePdfAsync
        [Test]
        public async Task GetInvoicePdfAsync_InvoiceNotFound_ReturnsNotFound()
        {
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>()));

            var result = await _service.GetInvoicePdfAsync(1, 5, new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Invoice not found");
        }

        [Test]
        public async Task GetInvoicePdfAsync_Unauthorized_WhenUserNotSellerAndNotAdmin()
        {
            var inv = MakeInvoice(1, 2, sellerId: 10, pdfUrl: "url");
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            var result = await _service.GetInvoicePdfAsync(1, 99, roles: null);
            result.StatusCode.Should().Be(StatusCodes.Forbidden);
            result.Message.Should().Be("Unauthorized access");
        }

        [Test]
        public async Task GetInvoicePdfAsync_PdfNotAvailable_ReturnsNotFound()
        {
            var inv = MakeInvoice(1, 2, sellerId: 10, pdfUrl: null);
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            var result = await _service.GetInvoicePdfAsync(1, 10, new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("PDF not available");
        }

        [Test]
        public async Task GetInvoicePdfAsync_DownloadFails_ReturnsInternalServerError()
        {
            var inv = MakeInvoice(1, 2, sellerId: 10, pdfUrl: "http://file");
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            _mockFileStorage
                .Setup(s => s.DownloadFileAsync(inv.PdfUrl!))
                .ThrowsAsync(new Exception("download err"));

            var result = await _service.GetInvoicePdfAsync(1, 10, new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Be("Error downloading PDF");
        }

        [Test]
        public async Task GetInvoicePdfAsync_Success_ReturnsPdfBytes()
        {
            var inv = MakeInvoice(1, 2, sellerId: 10, pdfUrl: "http://file");
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            var bytes = new byte[] { 5, 6 };
            _mockFileStorage
                .Setup(s => s.DownloadFileAsync(inv.PdfUrl!))
                .ReturnsAsync(bytes);

            var result = await _service.GetInvoicePdfAsync(1, 10, new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().BeEquivalentTo(bytes);
        }
        #endregion

        #region GetInvoiceByIdAsync
        [Test]
        public async Task GetInvoiceByIdAsync_NotFound_ReturnsNotFound()
        {
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>()));

            var result = await _service.GetInvoiceByIdAsync(1, 1, new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.NotFound);
            result.Message.Should().Be("Invoice not found");
        }

        [Test]
        public async Task GetInvoiceByIdAsync_Unauthorized_WhenNotAdminAndNotOwner()
        {
            var inv = MakeInvoice(3, 4, sellerId: 77);
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            var result = await _service.GetInvoiceByIdAsync(inv.Id, userId: 99, roles: new[] { "Seller" });
            result.StatusCode.Should().Be(StatusCodes.Forbidden);
        }

        [Test]
        public async Task GetInvoiceByIdAsync_AdminBypassesOwnership_ReturnsOk()
        {
            var inv = MakeInvoice(5, 6, sellerId: 77, pdfUrl: "url");
            _mockInvoiceRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<string[]>() ))
                .ReturnsAsync(inv);

            var result = await _service.GetInvoiceByIdAsync(inv.Id, userId: null, roles: new[] { "Admin" });
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(inv.Id);
            result.Data!.Order.Should().NotBeNull();
        }
        #endregion

        #region GetInvoicesAsync
        [Test]
        public void GetInvoicesAsync_NonAdminWithNullUserId_Throws()
        {
            var filter = new InvoiceFilterDto { PageNumber = 1, PageSize = 10 };
            Func<Task> act = () => _service.GetInvoicesAsync(filter, null, roles: new[] { "Seller" });
            act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetInvoicesAsync_NonAdmin_AppliesSellerFilterAndDateRangeAndOrderId()
        {
            var filter = new InvoiceFilterDto
            {
                PageNumber = 2,
                PageSize = 5,
                FromDate = new DateTime(2025, 1, 1),
                ToDate = new DateTime(2025, 12, 31),
                OrderId = 42
            };

            Expression<Func<Invoice, bool>>? capturedCriteriaForCount = null;
            Expression<Func<Invoice, bool>>? capturedCriteriaForFind = null;
            Expression<Func<Invoice, object>>? capturedSort = null;
            DATA.Constants.Enums.OrderBy capturedOrder = 0;

            _mockInvoiceRepo
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<bool>()))
                .Callback((Expression<Func<Invoice, bool>> c, bool _) => capturedCriteriaForCount = c)
                .ReturnsAsync(1);

            _mockInvoiceRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Invoice, bool>>>(),
                    filter.PageNumber,
                    filter.PageSize,
                    It.IsAny<string[]>(),
                    It.IsAny<Expression<Func<Invoice, object>>>(),
                    It.IsAny<DATA.Constants.Enums.OrderBy>(),
                    It.IsAny<bool>()))
                .Callback((Expression<Func<Invoice, bool>> c, int pn, int ps, string[] inc, Expression<Func<Invoice, object>> s, DATA.Constants.Enums.OrderBy ob, bool _) =>
                {
                    capturedCriteriaForFind = c; capturedSort = s; capturedOrder = ob;
                })
                .ReturnsAsync(new List<Invoice> { MakeInvoice(9, 42, sellerId: 10) });

            var res = await _service.GetInvoicesAsync(filter, 10, roles: new[] { "Seller" });

            res.StatusCode.Should().Be(StatusCodes.OK);
            res.Data.Should().NotBeNull();
            res.Data!.Items.Should().HaveCount(1);
            res.Data!.PageNumber.Should().Be(filter.PageNumber);
            res.Data!.PageSize.Should().Be(filter.PageSize);

            // Validate criteria behavior
            capturedCriteriaForCount.Should().NotBeNull();
            capturedCriteriaForFind.Should().NotBeNull();
            var func = capturedCriteriaForFind!.Compile();
            func(MakeInvoiceForCriteria(sellerId: 10, orderId: 42, date: new DateTime(2025, 6, 1))).Should().BeTrue();
            func(MakeInvoiceForCriteria(sellerId: 11, orderId: 42, date: new DateTime(2025, 6, 1))).Should().BeFalse();
            func(MakeInvoiceForCriteria(sellerId: 10, orderId: 99, date: new DateTime(2025, 6, 1))).Should().BeFalse();
            func(MakeInvoiceForCriteria(sellerId: 10, orderId: 42, date: new DateTime(2024, 12, 31))).Should().BeFalse();
            func(MakeInvoiceForCriteria(sellerId: 10, orderId: 42, date: new DateTime(2026, 1, 1))).Should().BeFalse();

            // Sorting should be by InvoiceDate descending
            capturedSort.Should().NotBeNull();
            capturedOrder.Should().Be(DATA.Constants.Enums.OrderBy.Descending);
        }

        [Test]
        public async Task GetInvoicesAsync_Admin_DoesNotApplySellerFilter_StillAppliesOtherFilters()
        {
            var filter = new InvoiceFilterDto
            {
                PageNumber = 1,
                PageSize = 10,
                FromDate = new DateTime(2025, 1, 1),
                ToDate = new DateTime(2025, 12, 31)
            };

            Expression<Func<Invoice, bool>>? capturedCriteria = null;
            _mockInvoiceRepo
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<bool>()))
                .Callback((Expression<Func<Invoice, bool>> c, bool _) => capturedCriteria = c)
                .ReturnsAsync(2);

            _mockInvoiceRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Invoice, bool>>>(),
                    filter.PageNumber,
                    filter.PageSize,
                    It.IsAny<string[]>(),
                    It.IsAny<Expression<Func<Invoice, object>>>(),
                    It.IsAny<DATA.Constants.Enums.OrderBy>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new List<Invoice> { MakeInvoice(1, 2, sellerId: 5), MakeInvoice(2, 3, sellerId: 6) });

            var res = await _service.GetInvoicesAsync(filter, userId: 10, roles: new[] { "Admin" });

            res.StatusCode.Should().Be(StatusCodes.OK);
            res.Data.Should().NotBeNull();
            res.Data!.Items.Should().HaveCount(2);

            // Ensure criteria matches date range but not necessarily seller id
            var func = capturedCriteria!.Compile();
            func(MakeInvoiceForCriteria(sellerId: 999, orderId: 2, date: new DateTime(2025, 2, 1))).Should().BeTrue();
            func(MakeInvoiceForCriteria(sellerId: 999, orderId: 2, date: new DateTime(2024, 12, 31))).Should().BeFalse();
        }
        #endregion
    }
}
