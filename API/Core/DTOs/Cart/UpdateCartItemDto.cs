using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Cart
{
    public class UpdateCartItemDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
