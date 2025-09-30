using DATA.Models.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class ProductImage : ISoftDeleteable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageURL { get; set; }

        [MaxLength(200)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
