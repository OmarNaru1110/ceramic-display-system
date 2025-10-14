using System;
using System.Collections.Generic;

namespace Core.DTOs.Cart
{
    public class GetCartDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<GetCartItemDto> Items { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class GetCartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerSqm { get; set; }
        public decimal ItemTotal { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
