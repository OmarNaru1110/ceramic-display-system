using Data.Models.Enums;
using System;
using System.Text.Json.Serialization;

namespace Core.DTOs.Order
{
    public class OrderFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus? Status { get; set; }

        public int? SellerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
