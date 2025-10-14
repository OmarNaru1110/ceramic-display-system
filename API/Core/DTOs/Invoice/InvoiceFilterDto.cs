using System;

namespace Core.DTOs.Invoice
{
    public class InvoiceFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? OrderId { get; set; }
    }
}
