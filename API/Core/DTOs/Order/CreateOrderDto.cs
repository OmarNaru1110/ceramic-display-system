using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerContact { get; set; }

        [Required]
        [MaxLength(500)]
        public string CustomerAddress { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public List<OrderItemDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
