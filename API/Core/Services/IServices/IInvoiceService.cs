using Core.DTOs.Invoice;
using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.IServices
{
    public interface IInvoiceService
    {
        Task<ResponseDto<GetInvoiceDto>> GenerateInvoiceAsync(int orderId);
        Task<ResponseDto<byte[]>> GetInvoicePdfAsync(int invoiceId, int? userId, string[]? roles);
        Task<ResponseDto<GetInvoiceDto>> GetInvoiceByIdAsync(int invoiceId, int? userId, string[]? roles);
        Task<ResponseDto<PagedResultDto<GetInvoiceDto>>> GetInvoicesAsync(InvoiceFilterDto filter, int? userId, string[]? roles);
    }
}
