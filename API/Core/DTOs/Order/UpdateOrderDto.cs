using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Order
{
    public class UpdateOrderDto
    {
        [MaxLength(200)]
        public string? CustomerName { get; set; }

        [MaxLength(100)]
        public string? CustomerContact { get; set; }

        [MaxLength(500)]
        public string? CustomerAddress { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public List<OrderItemDto>? Items { get; set; }
    }
}
