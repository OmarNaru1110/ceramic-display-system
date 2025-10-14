using System;

namespace Core.DTOs.Invoice
{
    public class GetInvoiceDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? PdfUrl { get; set; }

        public GetInvoiceOrderDto Order { get; set; }
    }

    public class GetInvoiceOrderDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerContact { get; set; }
        public string CustomerAddress { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
