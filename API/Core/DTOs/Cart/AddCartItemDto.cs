using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Cart
{
    public class AddCartItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
