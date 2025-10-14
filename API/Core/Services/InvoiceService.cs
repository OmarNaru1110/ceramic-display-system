using AutoMapper;
using Core.DTOs.Invoice;
using Core.Helpers;
using Core.Services.IServices;
using CORE.Constants;
using CORE.DTOs;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly IFileStorageService _fileStorage;

        public InvoiceService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InvoiceService> logger,
            IPdfGeneratorService pdfGenerator,
            IFileStorageService fileStorage)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _pdfGenerator = pdfGenerator;
            _fileStorage = fileStorage;
        }

        public async Task<ResponseDto<GetInvoiceDto>> GenerateInvoiceAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems", "OrderItems.Product" });
                if (order == null)
                    return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

                // Check if invoice already exists
                var existingInvoice = await _unitOfWork.Invoices.FindAsync(i => i.OrderId == orderId, null);
                if (existingInvoice != null)
                    return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.BadRequest, Message = "Invoice already exists" };

                var invoice = new Invoice
                {
                    OrderId = orderId,
                    TotalAmount = order.TotalAmount,
                    InvoiceDate = DateTime.UtcNow
                };

                await _unitOfWork.Invoices.AddOrUpdateAsync(invoice);
                await _unitOfWork.CommitAsync();

                // Generate PDF
                try
                {
                    var pdfBytes = await _pdfGenerator.GenerateInvoicePdfAsync(invoice, order, order.OrderItems.ToList());
                    var pdfUrl = await _fileStorage.UploadFileAsync(pdfBytes, $"invoice-{invoice.Id}.pdf", "invoices", "application/pdf");
                    
                    invoice.PdfUrl = pdfUrl;
                    await _unitOfWork.Invoices.AddOrUpdateAsync(invoice);
                    await _unitOfWork.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", invoice.Id);
                }

                return await GetInvoiceByIdAsync(invoice.Id, null, new[] { "Admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice");
                return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.InternalServerError, Message = "Error generating invoice" };
            }
        }

        public async Task<ResponseDto<byte[]>> GetInvoicePdfAsync(int invoiceId, int? userId, string[]? roles)
        {
            var invoice = await _unitOfWork.Invoices.FindAsync(i => i.Id == invoiceId, new[] { "Order" });
            if (invoice == null)
                return new ResponseDto<byte[]> { StatusCode = StatusCodes.NotFound, Message = "Invoice not found" };

            var isAdmin = roles?.Contains("Admin") == true;
            if (!isAdmin && invoice.Order.SellerId != userId)
                return new ResponseDto<byte[]> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            if (string.IsNullOrWhiteSpace(invoice.PdfUrl))
                return new ResponseDto<byte[]> { StatusCode = StatusCodes.NotFound, Message = "PDF not available" };

            try
            {
                var pdfBytes = await _fileStorage.DownloadFileAsync(invoice.PdfUrl);
                return new ResponseDto<byte[]> { StatusCode = StatusCodes.OK, Data = pdfBytes };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF");
                return new ResponseDto<byte[]> { StatusCode = StatusCodes.InternalServerError, Message = "Error downloading PDF" };
            }
        }

        public async Task<ResponseDto<GetInvoiceDto>> GetInvoiceByIdAsync(int invoiceId, int? userId, string[]? roles)
        {
            var invoice = await _unitOfWork.Invoices.FindAsync(i => i.Id == invoiceId, new[] { "Order" });
            if (invoice == null)
                return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.NotFound, Message = "Invoice not found" };

            var isAdmin = roles?.Contains("Admin") == true;
            if (!isAdmin && invoice.Order.SellerId != userId)
                return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            var invoiceDto = new GetInvoiceDto
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                TotalAmount = invoice.TotalAmount,
                InvoiceDate = invoice.InvoiceDate,
                PdfUrl = invoice.PdfUrl,
                Order = new GetInvoiceOrderDto
                {
                    Id = invoice.Order.Id,
                    CustomerName = invoice.Order.CustomerName,
                    CustomerContact = invoice.Order.CustomerContact,
                    CustomerAddress = invoice.Order.CustomerAddress,
                    OrderDate = invoice.Order.OrderDate
                }
            };

            return new ResponseDto<GetInvoiceDto> { StatusCode = StatusCodes.OK, Data = invoiceDto };
        }

        public async Task<ResponseDto<PagedResultDto<GetInvoiceDto>>> GetInvoicesAsync(InvoiceFilterDto filter, int? userId, string[]? roles)
        {
            var isAdmin = roles?.Contains("Admin") == true;

            System.Linq.Expressions.Expression<Func<Invoice, bool>> criteria = i => true;

            if (!isAdmin)
                criteria = i => i.Order.SellerId == userId.Value;

            if (filter.OrderId.HasValue)
                criteria = criteria.AndAlso(i => i.OrderId == filter.OrderId.Value);

            if (filter.FromDate.HasValue)
                criteria = criteria.AndAlso(i => i.InvoiceDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                criteria = criteria.AndAlso(i => i.InvoiceDate <= filter.ToDate.Value);

            var totalCount = await _unitOfWork.Invoices.CountAsync(criteria);
            var invoices = await _unitOfWork.Invoices.FindAsync(
                criteria,
                filter.PageNumber,
                filter.PageSize,
                new[] { "Order" },
                i => i.InvoiceDate,
                DATA.Constants.Enums.OrderBy.Descending
            );

            var invoiceDtos = invoices.Select(invoice => new GetInvoiceDto
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                TotalAmount = invoice.TotalAmount,
                InvoiceDate = invoice.InvoiceDate,
                PdfUrl = invoice.PdfUrl,
                Order = new GetInvoiceOrderDto
                {
                    Id = invoice.Order.Id,
                    CustomerName = invoice.Order.CustomerName,
                    CustomerContact = invoice.Order.CustomerContact,
                    CustomerAddress = invoice.Order.CustomerAddress,
                    OrderDate = invoice.Order.OrderDate
                }
            });

            return new ResponseDto<PagedResultDto<GetInvoiceDto>>
            {
                StatusCode = StatusCodes.OK,
                Data = new PagedResultDto<GetInvoiceDto>
                {
                    Items = invoiceDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                }
            };
        }
    }
}
