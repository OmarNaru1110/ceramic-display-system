using DATA.Models.Contract;
using Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Data.Models
{
    public class Product : ISoftDeleteable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public QualityGrade QualityGrade { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public ProductCategory Category { get; set; }

        [Required]
        public ProductType Type { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public Size Size { get; set; }

        [Required]
        public int PiecesPerBox { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerSqm { get; set; }

        [Required]
        public int AdminId { get; set; } // user : product 1:m

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties

        [ForeignKey("AdminId")]
        public virtual AppUser Admin { get; set; }

        public virtual Discount? Discount { get; set; } // product : discount 1:1
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>(); // product : product_image 1:m
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
    }
}