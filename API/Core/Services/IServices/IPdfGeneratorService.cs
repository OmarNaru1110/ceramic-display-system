using Core.Services.IServices;
using Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.IServices
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Order order, List<OrderItem> orderItems);
        Task<byte[]> GenerateReturnRequestPdfAsync(ReturnRequest returnRequest, Order order, List<ReturnItem> returnItems);
    }
}
