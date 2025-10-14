using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Data.Models.Enums;

namespace Core.DTOs.Order
{
    public class GetOrderDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerContact { get; set; }
        public string CustomerAddress { get; set; }
        public decimal TotalAmount { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; }

        public string? Notes { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<GetOrderItemDto> Items { get; set; }
        public GetInvoiceInfoDto? Invoice { get; set; }
    }

    public class GetOrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerSqm { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class GetInvoiceInfoDto
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? PdfUrl { get; set; }
    }
}
