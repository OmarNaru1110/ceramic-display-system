using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Data.Models.Enums;

namespace Core.DTOs.ReturnRequest
{
    public class GetReturnRequestDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ReturnReason { get; set; }
        public string? Description { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReturnStatus Status { get; set; }

        public DateTime RequestDate { get; set; }
        public string? AdminNotes { get; set; }
        public string? ReturnPdfUrl { get; set; }

        public List<GetReturnItemDto> Items { get; set; }
        public GetOrderInfoDto Order { get; set; }
    }

    public class GetReturnItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }

    public class GetOrderInfoDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
