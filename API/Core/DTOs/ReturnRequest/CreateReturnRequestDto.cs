using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.ReturnRequest
{
    public class CreateReturnRequestDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ReturnReason { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public List<ReturnItemDto> Items { get; set; }
    }

    public class ReturnItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
